using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Common.Client;

public sealed class SteamTools : ISteamTools
{
    public readonly ILogger _logger;

    /// <inheritdoc/>
    public string? SteamInstallPath { get; }

    public SteamTools(ILogger logger)
    {
        _logger = logger;
        SteamInstallPath = GetSteamInstallPath();
    }

    /// <inheritdoc/>
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
    private string? GetSteamInstallPath()
    {
        string? result;

        if (OperatingSystem.IsWindows())
        {
            result = GetWindowsInstallFolder();
        }
        else if (OperatingSystem.IsLinux())
        {
            result = GetLinuxInstallFolder();
        }
        else
        {
            throw new PlatformNotSupportedException("Can't identify platform");
        }

        if (result is null)
        {
            _logger.LogError("Can't find Steam install folder");
            return null;
        }

        _logger.LogInformation($"Using {result} as a Steam folder");
        return result;
    }

    /// <summary>
    /// Get Steam install folder on Windows
    /// </summary>
    private string? GetWindowsInstallFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        var path = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamPath", null);

        if (path is not string strPath)
        {
            return null;
        }

        var result = strPath.Replace('/', Path.DirectorySeparatorChar);

        if (!Directory.Exists(result))
        {
            _logger.LogError($"Steam install folder {result} doesn't exist");
            return null;
        }

        _logger.LogInformation($"Found Steam install folder at {result}");
        return result;
    }

    /// <summary>
    /// Get Steam install folder on Linux
    /// </summary>
    private string? GetLinuxInstallFolder()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string? result = null;

        //installer
        var result2 = Path.Combine(home, ".steam/steam");
        if (Directory.Exists(result2))
        {
            if (Directory.Exists(Path.Combine(result2, "steamapps")) &&
                File.Exists(Path.Combine(result2, "steamapps", "libraryfolders.vdf")))
            {
                _logger.LogInformation($"Found Steam install folder at {result2}");
                result = result2;
            }
        }

        //snap
        var result3 = Path.Combine(home, "snap/steam/common/.local/share/Steam");
        if (Directory.Exists(result3))
        {
            if (Directory.Exists(Path.Combine(result3, "steamapps")) &&
                File.Exists(Path.Combine(result3, "steamapps", "libraryfolders.vdf")))
            {
                _logger.LogInformation($"Found Steam install folder at {result3}");
                result = result3;
            }
        }

        //flatpak
        var result4 = Path.Combine(home, ".var/app/com.valvesoftware.Steam/.local/share/Steam");
        if (Directory.Exists(result4))
        {
            if (Directory.Exists(Path.Combine(result4, "steamapps")) &&
                File.Exists(Path.Combine(result4, "steamapps", "libraryfolders.vdf")))
            {
                _logger.LogInformation($"Found Steam install folder at {result4}");
                result = result4;
            }
        }

        //deck
        var result1 = Path.Combine(home, ".local/share/Steam");
        if (Directory.Exists(result1))
        {
            if (Directory.Exists(Path.Combine(result1, "steamapps")) &&
                File.Exists(Path.Combine(result1, "steamapps", "libraryfolders.vdf")))
            {
                _logger.LogInformation($"Found Steam install folder at {result1}");
                result = result1;
            }
        }

        if (result is null)
        {
            return null;
        }

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

