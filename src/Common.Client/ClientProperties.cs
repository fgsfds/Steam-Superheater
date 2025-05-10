using CommunityToolkit.Diagnostics;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace Common.Client;

public static class ClientProperties
{
    private static readonly SemaphoreSlim _semaphore = new(1);

    private static bool? _isSteamDeckGameMode;

    static ClientProperties()
    {
        WorkingFolder = Path.GetDirectoryName(Environment.ProcessPath)!;
        IsInSteamDeckGameMode = CheckDeckGameMode();
    }

    /// <summary>
    /// Current working folder of the app
    /// </summary>
    public static string WorkingFolder { get; }

    /// <summary>
    /// Is app started in developer mode
    /// </summary>
    public static bool IsDeveloperMode { get; set; }

    /// <summary>
    /// Current app version
    /// </summary>
    public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("999");

    /// <summary>
    /// Name of the executable file
    /// </summary>
    public static string ExecutableName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";
            }
            else if (OperatingSystem.IsLinux())
            {
                return AppDomain.CurrentDomain.FriendlyName;
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException();
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Is app run with admin privileges
    /// </summary>
    public static bool IsAdmin => OperatingSystem.IsWindows() &&
                                  new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    /// <summary>
    /// Is Game Mode active on Steam Deck
    /// </summary>
    public static bool IsInSteamDeckGameMode { get; set; }

    /// <summary>
    /// Is started in offline mode
    /// </summary>
    public static bool IsOfflineMode { get; set; }

    /// <summary>
    /// Check if Game Mode is active on Steam Deck
    /// </summary>
    private static bool CheckDeckGameMode()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        if (_isSteamDeckGameMode is not null)
        {
            return _isSteamDeckGameMode.Value;
        }

        _semaphore.Wait();

        ProcessStartInfo processInfo = new()
        {
            FileName = "bash",
            Arguments = "-c \"echo $DESKTOP_SESSION\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(processInfo);

        Guard.IsNotNull(proc);

        var result = proc.StandardOutput.ReadToEnd().Trim();

        proc.WaitForExit();

        _isSteamDeckGameMode = result.StartsWith("gamescope-wayland");

        _ = _semaphore.Release();

        return _isSteamDeckGameMode.Value;
    }

    public static string PathToLogFile => Path.Combine(WorkingFolder, "Superheater.log");
}
