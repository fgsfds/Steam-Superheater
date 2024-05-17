using Common.Entities;
using Common.Helpers;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Common
{
    public sealed class AppUpdateInstaller(
        ArchiveTools archiveTools,
        HttpClient httpClient,
        Logger logger
        )
    {
        private readonly ArchiveTools _archiveTools = archiveTools;
        private readonly HttpClient _httpClient = httpClient;
        private readonly Logger _logger = logger;

        private AppUpdateEntity? _update;

        /// <summary>
        /// Check API for releases with version higher than the current
        /// </summary>
        /// <param name="currentVersion">Current Superheater version</param>
        /// <returns>Has newer version</returns>
        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            _logger.Info("Checking for updates");

            string osName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                osName = OSPlatform.Windows.ToString().ToLower();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osName = OSPlatform.Linux.ToString().ToLower();
            }
            else
            {
                return false;
            }

            using var response = await _httpClient.GetAsync($"{ApiProperties.ApiUrl}/release/{osName}").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode || response.StatusCode is System.Net.HttpStatusCode.NoContent)
            {
                return false;
            }

            var releaseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var release = JsonSerializer.Deserialize(releaseJson, AppUpdateEntityContext.Default.AppUpdateEntity);

            if (release is not null && release.Version > currentVersion)
            {
                _logger.Info($"Found new version {release.Version}");

                _update = release;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Download latest release from Github and create update lock file
        /// </summary>
        public async Task DownloadAndUnpackLatestRelease(CancellationToken cancellationToken)
        {
            _logger.Info($"Downloading app update version {_update!.Version}");

            var updateUrl = _update.DownloadUrl;

            var fileName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(updateUrl.ToString()).Trim());

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            await _archiveTools.CheckAndDownloadFileAsync(updateUrl, fileName, cancellationToken).ConfigureAwait(false);

            ZipFile.ExtractToDirectory(fileName, Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFolder), true);

            File.Delete(fileName);

            await File.Create(Consts.UpdateFile).DisposeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Install update
        /// </summary>
        public static void InstallUpdate()
        {
            //_logger.Info("Starting app update");

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //starting new version of the app
                System.Diagnostics.Process.Start(oldExe);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //setting execute permission for user, otherwise the app won't run from game mode
               var attributes = File.GetUnixFileMode(oldExe);
               File.SetUnixFileMode(oldExe, attributes | UnixFileMode.UserExecute);
            }
            
            Environment.Exit(0);
        }
    }   
}
