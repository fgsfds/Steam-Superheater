using Common.Config;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Common.Helpers
{
    public sealed class CommonProperties(ConfigProvider configProvider)
    {
        private readonly ConfigEntity _config = configProvider.Config ?? ThrowHelper.NullReferenceException<ConfigEntity>(string.Empty);

        /// <summary>
        /// Path to local repository
        /// </summary>
        public string LocalRepoPath => _config.LocalRepoPath;

        /// <summary>
        /// Path to current repository (local or online)
        /// </summary>
        public string CurrentFixesRepo
        {
            get
            {
                var branch = _config.UseTestRepoBranch ? "test/" : "master/";

                return Consts.MainFixesRepo + "/raw/" + branch;
            }
        }

        /// <summary>
        /// Is Game Mode active on Steam Deck
        /// </summary>
        public bool IsInSteamDeckGameMode { get; } = CheckDeckGameMode();

        /// <summary>
        /// Current app version
        /// </summary>
        public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("999");

        /// <summary>
        /// Name of the executable file
        /// </summary>
        public static string ExecutableName => Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";

        public static bool IsAdmin
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                }

                return false;
            }
        }

        /// <summary>
        /// Check if Game Mode is active on Steam Deck
        /// </summary>
        private static bool CheckDeckGameMode()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ProcessStartInfo processInfo = new()
                {
                    FileName = "bash",
                    Arguments = "-c \"echo $DESKTOP_SESSION\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = Process.Start(processInfo) ?? ThrowHelper.Exception<Process>("Error starting process");

                var result = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit();

                Logger.Info($"DESKTOP_SESSION result {result}");

                if (result.StartsWith("gamescope-wayland"))
                {
                    Logger.Info("Steam game mode detected");
                    return true;
                }
            }

            return false;
        }
    }
}
