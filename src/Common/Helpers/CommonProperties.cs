using Common.Config;
using Common.DI;
using System.Reflection;

namespace Common.Helpers
{
    public class CommonProperties
    {
        public static string LocalRepo => BindingsManager.Instance.GetInstance<ConfigProvider>().Config.LocalRepoPath;

        public static Version CurrentVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static string ExecutableName => System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
    }
}
