using SteamFDCommon.Config;
using SteamFDCommon.DI;
using System.Reflection;

namespace SteamFDCommon.Helpers
{
    public class CommonProperties
    {
        public static string LocalRepo => BindingsManager.Instance.GetInstance<ConfigProvider>().Config.LocalRepoPath;

        public static Version CurrentVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static readonly Guid UpdaterGuid = new("92ef702d-04b0-42ff-8632-6a81285123e2");
    }
}
