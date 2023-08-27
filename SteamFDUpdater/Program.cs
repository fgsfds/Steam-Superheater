using System.Diagnostics;
using System.IO.Compression;

namespace SteamFDUpdater
{
    internal class Program
    {
        private static string _currentDir;
        private const string UpdateFile = ".update";

        private static readonly List<string> _keepFiles = new()
        {
            UpdateFile,
            "Updater.exe",
            "Updater.dll",
            "Updater.deps.json",
            "Updater.runtimeconfig.json",
            "Updater.pdb"
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Environment.Exit(0);
            }

            _currentDir = Directory.GetCurrentDirectory();

            while (Process.GetProcessesByName("SteamFD").Any())
            {
                Console.WriteLine("Waiting for SteamFD to exit...");
                Thread.Sleep(1000);
                Console.Clear();
            }

            if (File.Exists(".update"))
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

                    ZipFile.ExtractToDirectory(zip, _currentDir, true);

                    File.Delete(zip);
                    File.Delete(UpdateFile);
                }
            }
        }

        private static void RemoveOldFiles()
        {
            var files = Directory.GetFiles(_currentDir);

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