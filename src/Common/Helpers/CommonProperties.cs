﻿using Common.Config;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Common.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Helpers
{
    public static class CommonProperties
    {
        private static readonly ConfigEntity _config = BindingsManager.Provider.GetRequiredService<ConfigProvider>().Config;

        /// <summary>
        /// Path to current repository (local or online)
        /// </summary>
        public static string CurrentFixesRepo => Consts.MainFixesRepo + "/raw/" + (_config.UseTestRepoBranch ? "test/" : "master/");

        /// <summary>
        /// Current app version
        /// </summary>
        public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("999");

        /// <summary>
        /// Name of the executable file
        /// </summary>
        public static string ExecutableName => Process.GetCurrentProcess().MainModule?.ModuleName ?? "Superheater.exe";

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

            return false;
        }
    }
}
