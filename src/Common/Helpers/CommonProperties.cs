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

        public static string LocalRepoPath => _config.LocalRepoPath;

        public static string CurrentFixesRepo
        {
            get
            {
                var branch = _config.UseTestRepoBranch ? "test/" : "master/";

                return Consts.MainFixesRepo + "/raw/" + branch;
            }
        }

        public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("999");

        public static string ExecutableName => Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";

        public static bool IsInSteamDeckGameMode { get; }

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
