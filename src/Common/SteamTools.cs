using Microsoft.Win32;
using System.IO;

namespace Common
{
    public static class SteamTools
    {
        public static string? SteamInstallPath { get; }

        static SteamTools()
        {
            SteamInstallPath = GetSteamInstallPath();
        }

        /// <summary>
        /// Get list of ACF files from all Steam libraries
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAcfsList()
        {
            var libraries = GetSteamLibraries();

            List<string> result = new();

            foreach (var lib in libraries)
            {
                var path = Path.Combine(lib, "steamapps");

                var files = Directory.GetFiles(path, "*.acf");

                result.AddRange(files);
            }

            return result;
        }

        /// <summary>
        /// Get Steam install path
        /// </summary>
        /// <returns></returns>
        private static string? GetSteamInstallPath()
        {
            var path = (string?)Registry
                .GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath", null);

            if (path is null)
            {
                return null;
                //throw new Exception("Steam install folder can't be found.");
            }

            var fixedPath = path.Replace("/", "\\");

            return fixedPath;
        }

        /// <summary>
        /// Get list of Steam libraries
        /// </summary>
        /// <returns></returns>
        private static List<string> GetSteamLibraries()
        {
            List<string> result = new();

            var steamInstallPath = SteamInstallPath;

            if (steamInstallPath is null)
            {
                return result;
            }

            var libraryfolders = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");

            var lines = File.ReadAllLines(libraryfolders);

            foreach (var line in lines)
            {
                if (line.Contains("\"path\""))
                {
                    var l = line.Split('"');

                    var z = l.ElementAt(l.Length - 2).Trim();

                    if (Directory.Exists(z))
                    {
                        result.Add(z);
                    }
                }
            }

            return result;
        }
    }
}
