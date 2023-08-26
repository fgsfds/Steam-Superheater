using Newtonsoft.Json;

namespace SteamFDCommon.Updater
{
    public class GithubReleasesProvider
    {
        public static async Task ReadReleasesAsync(Version currentVersion)
        {
            GithubReleaseEntity.Root latestVersion;
            IEnumerable<GithubReleaseEntity.Root> newVersions;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("fgsfds");

                var a = await client.GetStringAsync("https://api.github.com/repos/fgsfds/Test-Repo/releases");

                var cc = JsonConvert.DeserializeObject<List<GithubReleaseEntity.Root>>(a);

                latestVersion = cc.Last();

                newVersions = cc.Where(x => new Version(x.tag_name) > currentVersion);
            }
        }
    }
}
