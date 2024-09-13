using Api.Common.Interface;
using Common.Client.FilesTools;
using Common.Client.Logger;
using Common.Entities;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using System.IO.Compression;

namespace Common.Client;

public sealed class AppUpdateInstaller(
    FilesDownloader filesDownloader,
    ApiInterface apiInterface,
    ILogger logger
    )
{
    private readonly FilesDownloader _filesDownloader = filesDownloader;
    private readonly ApiInterface _apiInterface = apiInterface;
    private readonly ILogger _logger = logger;

    private AppReleaseEntity? _update;

    /// <summary>
    /// Check API for releases with version higher than the current
    /// </summary>
    /// <param name="currentVersion">Current Superheater version</param>
    /// <returns>Has newer version</returns>
    public async Task<Result> CheckForUpdates(Version currentVersion)
    {
        _logger.Info("Checking for updates");

        var result = await _apiInterface.GetLatestAppReleaseAsync(OSEnumHelper.CurrentOSEnum).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new(result.ResultEnum, result.Message);
        }

        if (result.ResultObject is not null && result.ResultObject.Version > currentVersion)
        {
            _logger.Info($"Found new version {result.ResultObject.Version}");

            _update = result.ResultObject;

            return new(ResultEnum.Success, string.Empty);
        }

        return new(ResultEnum.NotFound, string.Empty);
    }

    /// <summary>
    /// Download latest release from Github and create update lock file
    /// </summary>
    public async Task DownloadAndUnpackLatestRelease(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(_update);

        _logger.Info($"Downloading app update version {_update.Version}");

        var updateUrl = _update.DownloadUrl;

        var fileName = Path.Combine(ClientProperties.WorkingFolder, Path.GetFileName(updateUrl.ToString()).Trim());

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        _ = await _filesDownloader.CheckAndDownloadFileAsync(updateUrl, fileName, cancellationToken).ConfigureAwait(false);

        ZipFile.ExtractToDirectory(fileName, Path.Combine(ClientProperties.WorkingFolder, Consts.UpdateFolder), true);

        File.Delete(fileName);

        await File.Create(Consts.UpdateFile).DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Install update
    /// </summary>
    public static void InstallUpdate()
    {
        //_logger.Info("Starting app update");

        var dir = ClientProperties.WorkingFolder;
        var updateDir = Path.Combine(dir, Consts.UpdateFolder);
        var oldExe = Path.Combine(dir, ClientProperties.ExecutableName);
        var newExe = Path.Combine(updateDir, ClientProperties.ExecutableName);

        //renaming old file
        File.Move(oldExe, oldExe + ".old", true);

        //moving new file
        File.Move(newExe, oldExe, true);

        File.Delete(Path.Combine(dir, Consts.UpdateFile));
        Directory.Delete(Path.Combine(dir, Consts.UpdateFolder), true);

        if (OperatingSystem.IsWindows())
        {
            //starting new version of the app
            _ = System.Diagnostics.Process.Start(oldExe);
        }
        else if (OperatingSystem.IsLinux())
        {
            //setting execute permission for user, otherwise the app won't run from game mode
            var attributes = File.GetUnixFileMode(oldExe);
            File.SetUnixFileMode(oldExe, attributes | UnixFileMode.UserExecute);
        }

        Environment.Exit(0);
    }
}

