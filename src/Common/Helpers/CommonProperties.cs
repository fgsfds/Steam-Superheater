using Common.Config;
using Common.DI;
using System.Reflection;

namespace Common.Helpers
{
    public sealed class CommonProperties
    {
        private static readonly ConfigEntity _config;

        static CommonProperties()
        {
            _config = BindingsManager.Instance.GetInstance<ConfigProvider>().Config;
        }

        public static string LocalRepoPath => _config.LocalRepoPath;

        public static Version CurrentVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static string ExecutableName => System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;

        public static string CurrentFixesRepo
        {
            get
            {
                var branch = _config.UseTestRepoBranch ? "test/" : "master/";

                return Consts.MainFixesRepo + "/raw/" + branch;
            }
        }
    }
}
