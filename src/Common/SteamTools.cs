    using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common
{
    internal static class SteamTools
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
            string? result;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var path = (string?)Registry
                .GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath", null);

                if (path is null)
                {
                    Logger.Error($"Can't find Steam install folder");
                    return null;
                }

                result = path.Replace('/', Path.DirectorySeparatorChar);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                result = Path.Combine(home, ".local/share/Steam");
            }
            else
            {
                return ThrowHelper.PlatformNotSupportedException<string>("Can't identify platform");
            }

            if (!Directory.Exists(result))
            {
                Logger.Error($"Steam install folder {result} doesn't exist");
                return null;
            }

            Logger.Info($"Steam install folder is {result}");
            return result;
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

            if (!File.Exists(libraryfolders))
            {
                return new();
            }

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
