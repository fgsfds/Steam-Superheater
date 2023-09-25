using Common.Entities;
using Common.Providers;
using Common.Helpers;
using System.IO.Compression;

namespace Common
{
    public class UpdateInstaller
    {
        private readonly List<UpdateEntity> _updates;

        public UpdateInstaller()
        {
            _updates = new();
        }

        /// <summary>
        /// Download latest release from Github and create update lock file
        /// </summary>
        /// <returns></returns>
        public async Task DownloadAndUnpackLatestRelease()
        {
            var fixUrl = _updates.OrderByDescending(x => x.Version).First().DownloadUrl;

            var fileName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(fixUrl.ToString()).Trim());

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            await FileTools.DownloadFileAsync(fixUrl, fileName);

            ZipFile.ExtractToDirectory(fileName, Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFolder), true);

            File.Delete(fileName);

            File.Create(Consts.UpdateFile).Dispose();
        }

        /// <summary>
        /// Install update
        /// </summary>
        public static void InstallUpdate()
        {
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

        /// <summary>
        /// Check github for releases with version higher than current
        /// </summary>
        /// <param name="currentVersion">Current SFD verson</param>
        /// <returns></returns>
        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            _updates.Clear();

            _updates.AddRange(await GithubReleasesProvider.GetNewerReleasesListAsync(currentVersion));

            return _updates.Any();
        }
    }
}
