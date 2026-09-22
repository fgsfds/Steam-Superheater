using System.IO.Compression;
using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Common.Client.FilesTools.Interfaces;
using Microsoft.Extensions.Logging;

namespace Common.Client;

public sealed class AppUpdateInstaller(
    IFilesDownloader filesDownloader,
    IApiInterface apiInterface,
    ILogger logger
    )
{
    private readonly IFilesDownloader _filesDownloader = filesDownloader;
    private readonly IApiInterface _apiInterface = apiInterface;
    private readonly ILogger _logger = logger;

    private AppReleaseEntity? _update;

    /// <summary>
    /// Check API for releases with version higher than the current
    /// </summary>
    /// <param name="currentVersion">Current Superheater version</param>
    /// <returns>Has newer version</returns>
    public async Task<Result> CheckForUpdates(Version currentVersion)
    {
        if (ClientProperties.IsOfflineMode)
        {
            return new(ResultEnum.NotFound, string.Empty);
        }

        _logger.LogInformation("Checking for updates");

        var result = await _apiInterface.GetLatestAppReleaseAsync(OSEnumHelper.CurrentOSEnum).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new(result.ResultEnum, result.Message);
        }

        if (result.ResultObject is not null && result.ResultObject.Version > currentVersion)
        {
            _logger.LogInformation($"Found new version {result.ResultObject.Version}");

            _update = result.ResultObject;

            return new(ResultEnum.Success, string.Empty);
        }

        return new(ResultEnum.NotFound, string.Empty);
    }

    /// <summary>
    /// Download latest release from Github and create update lock file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<Result> DownloadAndUnpackLatestRelease(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_update);

        _logger.LogInformation($"Downloading app update version {_update.Version}");

        var updateUrl = _update.DownloadUrl;

        var fileName = Path.Combine(ClientProperties.WorkingFolder, Path.GetFileName(updateUrl.ToString()).Trim());

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        var downloadResult = await _filesDownloader.DownloadFileAsync(updateUrl, fileName, cancellationToken).ConfigureAwait(false);

        if (!downloadResult.IsSuccess)
        {
            _logger.LogError($"Error while downloading app update: {downloadResult.Message}");

            return downloadResult;
        }

        try
        {
            ZipFile.ExtractToDirectory(fileName, Path.Combine(ClientProperties.WorkingFolder, ClientConstants.UpdateFolder), true);

            File.Delete(fileName);

            await File.Create(ClientConstants.UpdateFile).DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while unpacking app update");

            return new(ResultEnum.Error, "Error while unpacking app update");
        }

        return new(ResultEnum.Success, string.Empty);
    }

    /// <summary>
    /// Install update
    /// </summary>
    public static void InstallUpdate()
    {
        //_logger.Info("Starting app update");

        var dir = ClientProperties.WorkingFolder;
        var updateDir = Path.Combine(dir, ClientConstants.UpdateFolder);
        var oldExe = Path.Combine(dir, ClientProperties.ExecutableName);
        var newExe = Path.Combine(updateDir, ClientProperties.ExecutableName);

        //renaming old file
        File.Move(oldExe, oldExe + ".old", true);

        //moving new file
        File.Move(newExe, oldExe, true);

        File.Delete(Path.Combine(dir, ClientConstants.UpdateFile));
        Directory.Delete(Path.Combine(dir, ClientConstants.UpdateFolder), true);

        if (OperatingSystem.IsLinux())
        {
            //setting execute permission for user, otherwise the app won't run from game mode
            var attributes = File.GetUnixFileMode(oldExe);
            File.SetUnixFileMode(oldExe, attributes | UnixFileMode.UserExecute);
        }

        //starting new version of the app
        _ = System.Diagnostics.Process.Start(oldExe);

        Environment.Exit(0);
    }
}

