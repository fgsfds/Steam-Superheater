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
            IsSteamGameMode = CheckDeckGameMode();
        }

        public static string LocalRepoPath => _config.LocalRepoPath;

        public static Version CurrentVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static string ExecutableName => Process.GetCurrentProcess().MainModule.ModuleName;

        public static string CurrentFixesRepo
        {
            get
            {
                var branch = _config.UseTestRepoBranch ? "test/" : "master/";

                return Consts.MainFixesRepo + "/raw/" + branch;
            }
        }

        public static bool IsSteamGameMode { get; }

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

                var proc = Process.Start(processInfo);

                var result = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit();

                Logger.Log($"DESKTOP_SESSION result {result[..2]}");

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
