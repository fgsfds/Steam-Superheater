using System.Diagnostics;
using System.Security.Cryptography;
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
            await StageUpdateAsync(fileName, ClientProperties.WorkingFolder, ClientProperties.ExecutableName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while unpacking app update");

            return new(ResultEnum.Error, "Error while unpacking app update");
        }

        return new(ResultEnum.Success, string.Empty);
    }

    internal static async Task StageUpdateAsync(string fileName, string directory, string executableName, CancellationToken cancellationToken)
    {
        var staging = Path.Combine(directory, ClientConstants.UpdateFolder);
        _ = Directory.CreateDirectory(staging);
        var executable = Path.Combine(staging, executableName);
        using (var zip = ZipFile.OpenRead(fileName))
        {
            var entry = zip.GetEntry(executableName)
                ?? throw new InvalidDataException("Update archive does not contain the application executable.");
            entry.ExtractToFile(executable, true);
        }
        ValidateExecutable(executable);
        var checksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(executable)));
        await File.WriteAllTextAsync(Path.Combine(directory, ClientConstants.UpdateFile), checksum, cancellationToken).ConfigureAwait(false);
        File.Delete(fileName);
    }

    /// <summary>Applies the staged update and restarts with the original launch arguments.</summary>
    public static void InstallUpdate(string[] arguments, Action releaseOwnership)
    {
        ApplyStagedUpdate(ClientProperties.WorkingFolder, ClientProperties.ExecutableName, path =>
        {
            var start = new ProcessStartInfo(path) { WorkingDirectory = ClientProperties.WorkingFolder, UseShellExecute = false };
            foreach (var argument in arguments) { start.ArgumentList.Add(argument); }
            releaseOwnership();
            using var process = Process.Start(start) ?? throw new IOException("Unable to start the updated application.");
        });
    }

    internal static void ApplyStagedUpdate(string directory, string executable, Action<string> launch)
    {
        var current = Path.Combine(directory, executable);
        var staged = Path.Combine(directory, ClientConstants.UpdateFolder, executable);
        var marker = Path.Combine(directory, ClientConstants.UpdateFile);
        var checksum = File.ReadAllText(marker).Trim();
        ValidateExecutable(staged);
        if (!checksum.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(staged))), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Staged update checksum does not match. Download the update again.");
        }
        if (OperatingSystem.IsLinux()) { File.SetUnixFileMode(staged, File.GetUnixFileMode(staged) | UnixFileMode.UserExecute); }
        var backup = current + ".old";
        var moved = false;
        try
        {
            File.Move(current, backup);
            moved = true;
            File.Move(staged, current);
            File.Delete(marker);
            launch(current);
        }
        catch
        {
            if (moved)
            {
                if (File.Exists(current)) { File.Move(current, staged, true); }
                File.Move(backup, current);
                File.WriteAllText(marker, checksum);
            }
            throw;
        }
    }

    private static void ValidateExecutable(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[4];
        if (stream.Read(header) != 4 || !(header[..2].SequenceEqual("MZ"u8) || header.SequenceEqual(new byte[] { 0x7f, (byte)'E', (byte)'L', (byte)'F' })))
        {
            throw new InvalidDataException("The staged application is not a supported executable.");
        }
    }
}
