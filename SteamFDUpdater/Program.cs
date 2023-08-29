using System.Diagnostics;
using System.IO.Compression;

namespace SteamFDUpdater
{
    internal class Program
    {
        private static string CurrentDir => Directory.GetCurrentDirectory();
        private const string UpdateFile = ".update";

        private static readonly List<string> _keepFiles = new()
        {
            UpdateFile,
            "Updater.exe",
            "Updater.dll",
            "Updater.deps.json",
            "Updater.runtimeconfig.json"
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Environment.Exit(0);
            }

            while (Process.GetProcessesByName("SteamFD").Any())
            {
                Console.WriteLine("Waiting for SteamFD to exit...");
                Thread.Sleep(1000);
                Console.Clear();
            }

            if (File.Exists(UpdateFile))
            {
                string zip;

                using (var reader = new StreamReader(UpdateFile))
                {
                    zip = reader.ReadToEnd().Trim();
                    _keepFiles.Add(zip);
                }

                var files = args.First().Split(';');
                _keepFiles.AddRange(files);

                if (File.Exists(zip))
                {
                    RemoveOldFiles();

                    ZipFile.ExtractToDirectory(zip, CurrentDir, true);

                    File.Delete(zip);
                    File.Delete(UpdateFile);
                }
            }
        }

        private static void RemoveOldFiles()
        {
            var files = Directory.GetFiles(CurrentDir);

            foreach (var file in files)
            {
                if (_keepFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }
}