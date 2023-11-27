using Common.Entities;
using Common.Helpers;
using Common.Providers;
using System.IO.Compression;

namespace Common
{
    public sealed class AppUpdateInstaller
    {
        private List<AppUpdateEntity> _updates = [];

        /// <summary>
        /// Check GitHub for releases with version higher than current
        /// </summary>
        /// <param name="currentVersion">Current SFD version</param>
        /// <returns></returns>
        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            Logger.Info("Checking for updates");

            _updates = (await GitHubReleasesProvider.GetNewerReleasesListAsync(currentVersion)).ToList();

            Logger.Info($"Found {_updates.Count} updates");

            return _updates.Count != 0;
        }

        /// <summary>
        /// Download latest release from Github and create update lock file
        /// </summary>
        /// <returns></returns>
        public async Task DownloadAndUnpackLatestRelease()
        {
            AppUpdateEntity latestUpdate = _updates.MaxBy(static x => x.Version) ?? ThrowHelper.NullReferenceException<AppUpdateEntity>("Error while getting newer release");

            Logger.Info($"Downloading app update version {latestUpdate.Version}");

            var updateUrl = latestUpdate.DownloadUrl;

            var fileName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(updateUrl.ToString()).Trim());

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            await FileTools.CheckAndDownloadFileAsync(updateUrl, fileName);

            ZipFile.ExtractToDirectory(fileName, Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFolder), true);

            File.Delete(fileName);

            await File.Create(Consts.UpdateFile).DisposeAsync();
        }

        /// <summary>
        /// Install update
        /// </summary>
        public static void InstallUpdate()
        {
            Logger.Info("Starting app update");

            var dir = Directory.GetCurrentDirectory();
            var updateDir = Path.Combine(dir, Consts.UpdateFolder);
            var oldExe = Path.Combine(dir, CommonProperties.ExecutableName);
            var newExe = Path.Combine(updateDir, CommonProperties.ExecutableName);

            //renaming old file
            File.Move(oldExe, oldExe + ".old", true);

            //moving new file
            File.Move(newExe, oldExe, true);

            File.Delete(Path.Combine(dir, Consts.UpdateFile));
            Directory.Delete(Path.Combine(dir, Consts.UpdateFolder), true);

            var exeName = Path.Combine(Directory.GetCurrentDirectory(), CommonProperties.ExecutableName);
            System.Diagnostics.Process.Start(exeName);
            Environment.Exit(0);
        }
    }
}
