using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.Client
{
    public class SteamTools
    {
        public readonly Logger _logger;

        public SteamTools(Logger logger)
        {
            _logger = logger;
            SteamInstallPath = GetSteamInstallPath();
        }

        public string? SteamInstallPath { get; }

        /// <summary>
        /// Get list of ACF files from all Steam libraries
        /// </summary>
        /// <returns></returns>
        public List<string> GetAcfsList()
        {
            var libraries = GetSteamLibraries();

            List<string> result = new(100);

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
        private string? GetSteamInstallPath()
        {
            string? result;

            if (OperatingSystem.IsWindows())
            {
                var path = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", null);

                if (path is not string strPath)
                {
                    _logger.Error("Can't find Steam install folder");
                    return null;
                }

                result = strPath.Replace('/', Path.DirectorySeparatorChar);
            }
            else if (OperatingSystem.IsLinux())
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
                _logger.Error($"Steam install folder {result} doesn't exist");
                return null;
            }

            _logger.Info($"Steam install folder is {result}");
            return result;
        }

        /// <summary>
        /// Get list of Steam libraries
        /// </summary>
        /// <returns>List of paths to Steam libraries</returns>
        private List<string> GetSteamLibraries()
        {
            var steamInstallPath = SteamInstallPath;

            if (steamInstallPath is null)
            {
                return [];
            }

            var libraryfolders = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryfolders))
            {
                return [];
            }

            List<string> result = [];

            var lines = File.ReadAllLines(libraryfolders);

            foreach (var line in lines)
            {
                if (!line.Contains("\"path\""))
                {
                    continue;
                }

                var dirLine = line.Split('"');

                var dir = dirLine.ElementAt(dirLine.Length - 2).Trim();

                if (Directory.Exists(dir))
                {
                    result.Add(dir);
                }
            }

            return result;
        }
    }
}
