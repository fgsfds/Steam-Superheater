using SteamFDCommon;
using System.Diagnostics;
using System.IO.Compression;

namespace SteamFDUpdater
{
    internal class Program
    {
        private static string _currentDir;

        private static readonly List<string> _keepFiles = new()
        {
            Consts.ConfigFile,
            Consts.InstalledFile,
            Consts.UpdateFile,
            "Updater.exe",
            "Updater.dll",
            "Updater.deps.json",
            "Updater.runtimeconfig.json",
            "Updater.pdb"
        };

        static void Main()
        {
            _currentDir = Directory.GetCurrentDirectory();

            while (Process.GetProcessesByName("SteamFD").Any())
            {
                Console.WriteLine("Waiting for SteamFD to exit...");
                Thread.Sleep(100);
                Console.Clear();
            }

            if (File.Exists(".update"))
            {
                Console.WriteLine("OK");

                string zip;

                using (var reader = new StreamReader(Consts.UpdateFile))
                {
                    zip = reader.ReadToEnd().Trim();
                    _keepFiles.Add(zip);
                }

                if (File.Exists(zip))
                {
                    RemoveOldFiles();

                    ZipFile.ExtractToDirectory(zip, _currentDir, true);

                    File.Delete(zip);
                    File.Delete(Consts.UpdateFile);
                }
            }
            else
            {
                Console.WriteLine("Not OK");
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