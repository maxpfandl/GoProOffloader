using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace GoProOffloader
{
    internal class Program
    {
        static string _source = "";
        static string _destination = "";
        static IConfiguration? _config;

        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            _config = builder.Build();

            if (args.Length == 2)
            {
                _source = args[0];
                _destination = args[1];
            }
            else
            {
                _source = _config["defaultSource"]!;
                _destination = _config["defaultDestination"]!;
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

            if (camera == "unknown" || camera == null || camera == "")
            {
                Console.WriteLine("Unknown camera type, not a GoPro? Please enter camera type or press enter to continue without camera type:");
                camera = Console.ReadLine() ?? "";
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
                    string dest = "";
                    if (!string.IsNullOrEmpty(camera))
                        dest = System.IO.Path.Combine(_destination, creation.ToString("yyyy-MM-dd"), camera, creation.ToString("HHmmss") + "_" + name);
                    else
                        dest = System.IO.Path.Combine(_destination, creation.ToString("yyyy-MM-dd"), creation.ToString("HHmmss") + "_" + name);

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
            try
            {
                var versionFile = System.IO.Path.Combine(source, "misc", "version.txt");
                if (!System.IO.File.Exists(versionFile))
                {
                    return "unknown";
                }

                using FileStream stream = File.OpenRead(versionFile);
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                };
                var version = JsonSerializer.Deserialize<GoProVersion>(stream, options);

                return version?.Camera ?? "unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading version file: " + ex.Message);
                return "unknown";
            }
        }

        private static List<string> GetMediaFilesRecursive(string path)
        {
            List<string> files = new List<string>();
            files.AddRange(System.IO.Directory.GetFiles(path, "*.jpg"));
            files.AddRange(System.IO.Directory.GetFiles(path, "*.mp4"));
            files.AddRange(System.IO.Directory.GetFiles(path, "*.cr2"));

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