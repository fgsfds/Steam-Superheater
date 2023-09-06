using SteamFDCommon.Config;
using SteamFDCommon.DI;
using System.Reflection;

namespace SteamFDCommon
{
    public class Consts
    {
        public const string ConfigFile = "config.xml";

        public const string FixesFile = "fixes.xml";

        public const string NewsFile = "news.xml";

        public const string InstalledFile = "installed.xml";

        public static string LocalRepo => BindingsManager.Instance.GetInstance<ConfigProvider>().Config.LocalRepoPath;

        public const string GitHubRepo = "https://github.com/fgsfds/SteamFD-Fixes-Repo/raw/master/";

        public const string GitHubReleases = "https://api.github.com/repos/fgsfds/Steam-Superheater/releases";

        public const string PCGamingWikiUrl = "https://pcgamingwiki.com/api/appid.php?appid=";

        public const string AdminRegistryKey = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

        public const string BackupFolder = ".sfd";

        public const string UpdateFile = ".update";

        public const string UpdaterExe = "Updater.exe";

        public static readonly Guid UpdaterGuid = new("92ef702d-04b0-42ff-8632-6a81285123e2");

        public static Version CurrentVersion => Assembly.GetEntryAssembly().GetName().Version;
    }
}
