﻿using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace GoProOffloader
{
    internal class Program
    {
        static string _source = "";
        static string _destination = "";
        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            if (args.Length == 2)
            {
                _source = args[0];
                _destination = args[1];
            }
            else
            {
                _source = config["defaultSource"]!;
                _destination = config["defaultDestination"]!;
            }

            Console.WriteLine("Source: " + _source);
            Console.WriteLine("Destination: " + _destination);

            if (System.IO.Directory.Exists(_source) == false)
            {
                Console.WriteLine("Source directory does not exist");
                Console.ReadLine();
                return;
            }

            if (System.IO.Directory.Exists(Path.Combine(_source, "dcim")) == false)
            {
                Console.WriteLine("Source does not contain a DCIM directory");
                Console.ReadLine();
                return;
            }


            CopyFiles();
            Console.Write("Done, press any key to exit");
            Console.ReadLine();
        }

        public static void CopyFiles()
        {
            string camera = GetCamera(_source);

            if (camera == "Unknown")
            {
                Console.WriteLine("Unknown camera type, not a GoPro?");
                return;
            }

            List<string> files = GetMediaFilesRecursive(Path.Combine(_source, "DCIM"));
            if (files.Count == 0)
            {
                Console.WriteLine("No files found");
                return;
            }
            foreach (string file in files)
            {
                try
                {
                    DateTime creation = File.GetCreationTime(file);
                    string name = System.IO.Path.GetFileName(file);

                    string dest = System.IO.Path.Combine(_destination, creation.ToString("yyyy-MM-dd"), camera, creation.ToString("HHmmss") + "_" + name);

                    if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    }
                    if (System.IO.File.Exists(dest))
                    {
                        Console.WriteLine(dest + ": File already exists, skipping");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(dest);
                        System.IO.File.Copy(file, dest, false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(file + ": Error copying file: " + ex.Message);
                }

            }
        }

        private static string GetCamera(string source)
        {
            using FileStream stream = File.OpenRead((System.IO.Path.Combine(source, "misc", "version.txt")));
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            };
            var version = JsonSerializer.Deserialize<GoProVersion>(stream, options);

            return version?.Camera ?? "Unknown";
        }

        private static List<string> GetMediaFilesRecursive(string path)
        {
            List<string> files = new List<string>();
            files.AddRange(System.IO.Directory.GetFiles(path, "*.jpg"));
            files.AddRange(System.IO.Directory.GetFiles(path, "*.mp4"));

            foreach (string dir in System.IO.Directory.GetDirectories(path))
            {
                files.AddRange(GetMediaFilesRecursive(dir));
            }
            return files;
        }


    }

    public class GoProVersion
    {
        [JsonPropertyName("camera type")]
        public string? Camera { get; set; }
    }
}