using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Common.Helpers
{
    public static class CommonProperties
    {
        private static readonly SemaphoreSlim _semaphore = new(1);

        private static bool? _isSteamDeckGameMode = null;

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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return AppDomain.CurrentDomain.FriendlyName;
                }
                else
                {
                    ThrowHelper.PlatformNotSupportedException();
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Is app run with admin privileges
        /// </summary>
        public static bool IsAdmin => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                                      new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        /// <summary>
        /// Is Game Mode active on Steam Deck
        /// </summary>
        public static bool IsInSteamDeckGameMode => CheckDeckGameMode();

        /// <summary>
        /// Check if Game Mode is active on Steam Deck
        /// </summary>
        private static bool CheckDeckGameMode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return false;
            }

            _semaphore.Wait();

            if (_isSteamDeckGameMode is not null)
            {
                _semaphore.Release();
                return _isSteamDeckGameMode.Value;
            }

            ProcessStartInfo processInfo = new()
            {
                FileName = "bash",
                Arguments = "-c \"echo $DESKTOP_SESSION\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(processInfo) ?? ThrowHelper.Exception<Process>("Error while starting process");

            var result = proc.StandardOutput.ReadToEnd().Trim();

            proc.WaitForExit();

            //Logger.Info($"DESKTOP_SESSION result {result}");

            if (result.StartsWith("gamescope-wayland"))
            {
                //.Info("Steam game mode detected");

                _isSteamDeckGameMode = true;
            }
            else
            {
                _isSteamDeckGameMode = false;
            }

            _semaphore.Release();
            return _isSteamDeckGameMode.Value;
        }
    }
}
