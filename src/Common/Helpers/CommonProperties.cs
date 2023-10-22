using Common.Config;
using Common.DI;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Common.Helpers
{
    public sealed class CommonProperties
    {
        private static readonly ConfigEntity _config;

        static CommonProperties()
        {
            _config = BindingsManager.Instance.GetInstance<ConfigProvider>().Config;
            IsInSteamDeckGameMode = CheckDeckGameMode();
        }

        /// <summary>
        /// Path to local repository
        /// </summary>
        public static string LocalRepoPath => _config.LocalRepoPath;

        /// <summary>
        /// Path to current repository (local or online)
        /// </summary>
        public static string CurrentFixesRepo
        {
            get
            {
                var branch = _config.UseTestRepoBranch ? "test/" : "master/";

                return Consts.MainFixesRepo + "/raw/" + branch;
            }
        }

        /// <summary>
        /// Current app version
        /// </summary>
        public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("999");

        /// <summary>
        /// Name of the executable file
        /// </summary>
        public static string ExecutableName => Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";

        /// <summary>
        /// Is Game Mode active on Steam Deck
        /// </summary>
        public static bool IsInSteamDeckGameMode { get; }

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

                var proc = Process.Start(processInfo) ?? throw new Exception("Error starting process");

                var result = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit();

                Logger.Log($"DESKTOP_SESSION result {result}");

                if (result.StartsWith("gamescope-wayland"))
                {
                    Logger.Log("Steam game mode detected");
                    return true;
                }
            }

            return false;
        }
    }
}
