using System.Diagnostics;
using System.Security.Cryptography;
using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Octodiff.Core;

namespace Common.Client.FixTools.FileFix;

public sealed class FileFixInstaller
{
    private readonly IConfigProvider _configEntity;
    private readonly ArchiveTools _archiveTools;
    private readonly IFilesDownloader _filesDownloader;
    private readonly ProgressReport _progressReport;
    private readonly ILogger _logger;
    private readonly IApiInterface _apiInterface;
    private readonly IFixesProvider _fixesProvider;


    public FileFixInstaller(
        IConfigProvider config,
        ArchiveTools archiveTools,
        IFilesDownloader filesDownloader,
        ProgressReport progressReport,
        ILogger logger,
        IApiInterface apiInterface,
        IFixesProvider fixesProvider
        )
    {
        _configEntity = config;
        _archiveTools = archiveTools;
        _filesDownloader = filesDownloader;
        _progressReport = progressReport;
        _logger = logger;
        _apiInterface = apiInterface;
        _fixesProvider = fixesProvider;
    }


    /// <summary>
    /// Install file fix: download ZIP, backup and delete files if needed, run post install events
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <param name="fix">Fix entity</param>
    /// <param name="variant">Fix variant</param>
    /// <param name="skipMD5Check">Skip archive SHA-256 verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="Exception">Error while downloading file</exception>
    public async Task<Result<BaseInstalledFixEntity>> InstallFixAsync(
        GameEntity game,
        FileFixEntity fix,
        string? variant,
        bool skipMD5Check,
        CancellationToken cancellationToken
        )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(ResultEnum.Cancelled, null, "Installation cancelled");
        }

        //checking if compdata folder exists
        //compdata contains proton settings required for wine dll overrides
        if (fix.WineDllOverrides is not null && OperatingSystem.IsLinux())
        {
            if (!File.Exists($"{Environment.GetEnvironmentVariable("HOME")}/.local/share/Steam/steamapps/compatdata/{game.Id}/pfx/user.reg"))
            {
                return new(
                    ResultEnum.Error,
                    null,
                    """
                        Can't find 'compatdata' folder.
                        
                        Run the game at least once before installing this fix.
                        """);
            }
        }

        var preparedArchive = await PrepareArchiveAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
        if (!preparedArchive.IsSuccess)
        {
            return new(preparedArchive.ResultEnum, null, preparedArchive.Message);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new(ResultEnum.Cancelled, null, "Installation cancelled");
        }

        var sharedFixInstallResult = await InstallSharedFixAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

        if (!sharedFixInstallResult.IsSuccess)
        {
            return sharedFixInstallResult;
        }

        var backupFolderPath = CreateAndGetBackupFolder(game.InstallDir, fix.Name);

        BackupFiles(fix.FilesToDelete, game.InstallDir, backupFolderPath, true);
        BackupFiles(fix.FilesToBackup, game.InstallDir, backupFolderPath, false);
        BackupFiles(fix.FilesToPatch, game.InstallDir, backupFolderPath, true);

        var filesUnpackResult = await UnpackPreparedArchiveAsync(game, variant, backupFolderPath, preparedArchive.ResultObject).ConfigureAwait(false);

        if (!filesUnpackResult.IsSuccess)
        {
            return new(
                filesUnpackResult.ResultEnum,
                null,
                filesUnpackResult.Message);
        }

        await PatchFilesAsync(fix.FilesToPatch, game.InstallDir, backupFolderPath).ConfigureAwait(false);

        var dllOverrides = await ApplyWineDllOverridesAsync(game.Id, fix.WineDllOverrides).ConfigureAwait(false);

        RunAfterInstall(game.InstallDir, fix.RunAfterInstall);

        FileInstalledFixEntity installedFix = new()
        {
            GameId = game.Id,
            Guid = fix.Guid,
            Version = fix.Version,
            BackupFolder = Directory.Exists(backupFolderPath) ? new DirectoryInfo(backupFolderPath).Name : null,
            InstalledSharedFix = (FileInstalledFixEntity)sharedFixInstallResult.ResultObject,
            WineDllOverrides = dllOverrides,
            FilesList = filesUnpackResult.ResultObject,
            BuildId = game.BuildId,
        };

        return new(ResultEnum.Success, installedFix, "Successfully installed fix");
    }

    private async Task<Result<BaseInstalledFixEntity>> InstallSharedFixAsync(
        GameEntity game,
        FileFixEntity fix,
        string? variant,
        bool skipMD5Check,
        CancellationToken cancellationToken
        )
    {
        if (fix.SharedFix is null)
        {
            return new(ResultEnum.Success, null, "No shared fixes");
        }

        fix.SharedFix.InstallFolder = fix.SharedFixInstallFolder;

        var installedSharedFix = await InstallFixAsync(game, fix.SharedFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

        return installedSharedFix;
    }

    /// <summary>
    /// Checks an update archive before the currently installed fix is removed.
    /// </summary>
    internal async Task<Result> ValidateArchiveAsync(
        GameEntity game, FileFixEntity fix, string? variant, bool skipHashCheck, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(ResultEnum.Cancelled, "Verification cancelled");
        }

        var prepared = await PrepareArchiveAsync(game, fix, variant, skipHashCheck, cancellationToken).ConfigureAwait(false);
        return cancellationToken.IsCancellationRequested
            ? new(ResultEnum.Cancelled, "Verification cancelled")
            : new(prepared.ResultEnum, prepared.Message);
    }
    private sealed record PreparedArchive(string Path, string Destination, Dictionary<string, long?> Files);

    private async Task<Result<PreparedArchive?>> PrepareArchiveAsync(
        GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken)
    {
        if (fix.Url is null)
        {
            return new(ResultEnum.Success, null, string.Empty);
        }

        var destination = ClientHelpers.GetFullPath(game.InstallDir, fix.InstallFolder);
        var download = await CheckAndDownloadFileAsync(new Uri(fix.Url), fix.Guid, fix.Sha256, skipMD5Check, cancellationToken).ConfigureAwait(false);
        if (!download.IsSuccess)
        {
            return new(download.ResultEnum, null, download.Message);
        }

        var files = _archiveTools.GetListOfFilesInArchive(download.ResultObject, destination, fix.InstallFolder, variant);
        return new(ResultEnum.Success, new PreparedArchive(download.ResultObject, destination, files), string.Empty);
    }

    private async Task<Result<Dictionary<string, long?>?>> UnpackPreparedArchiveAsync(
        GameEntity game, string? variant, string backupFolderPath, PreparedArchive? archive)
    {
        if (archive is null)
        {
            return new(ResultEnum.Success, null, "Fix doesn't have downloadable file");
        }

        BackupFiles([.. archive.Files.Keys], game.InstallDir, backupFolderPath, true);
        await _archiveTools.UnpackArchiveAsync(archive.Path, archive.Destination, variant).ConfigureAwait(false);

        if (_configEntity.DeleteZipsAfterInstall &&
            !_configEntity.UseLocalApiAndRepo &&
            archive.Path.StartsWith(ClientProperties.WorkingFolder, StringComparison.Ordinal))
        {
            File.Delete(archive.Path);
        }

        return new(ResultEnum.Success, archive.Files, string.Empty);
    }
    /// <summary>
    /// Add dll overrides to the registry
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="dllList">List of DLLs to add</param>
    /// <returns>List of added lines</returns>
    private async Task<List<string>?> ApplyWineDllOverridesAsync(
        int gameId,
        List<string>? dllList
        )
    {
        if (dllList is null || !OperatingSystem.IsLinux())
        {
            return null;
        }

        var userRegFile = $"{Environment.GetEnvironmentVariable("HOME")}/.local/share/Steam/steamapps/compatdata/{gameId}/pfx/user.reg";

        var userRegLines = (await File.ReadAllLinesAsync(userRegFile).ConfigureAwait(false)).ToList();

        var startIndex = userRegLines.FindIndex(static x => x.Contains(@"[Software\\Wine\\DllOverrides]"));

        List<string> addedLines = new(dllList.Count);

        foreach (var dll in dllList)
        {
            var line = @$"""{dll}""=""native,builtin""";

            addedLines.Add(line);
        }

        userRegLines.InsertRange(startIndex + 1, addedLines);
        await File.WriteAllLinesAsync(userRegFile, userRegLines).ConfigureAwait(false);

        return addedLines;
    }

    private async Task<Result<string>> CheckAndDownloadFileAsync(
        Uri fixUrl, Guid fixGuid, string? expectedHash, bool skipHashCheck, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(ResultEnum.Cancelled, null, "Verification cancelled");
        }

        if (!skipHashCheck && !IsSha256Hash(expectedHash))
        {
            return new(ResultEnum.HashError, null, "The fix does not have a valid SHA-256 hash. Verification cannot proceed.");
        }

        var pathToArchive = fixUrl.IsFile
            ? fixUrl.LocalPath
            : _configEntity.UseLocalApiAndRepo
                ? Path.Combine(_configEntity.LocalRepoPath, "fixes", Path.GetFileName(fixUrl.LocalPath))
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fixUrl.LocalPath));

        if (File.Exists(pathToArchive))
        {
            if (skipHashCheck)
            {
                _logger.LogWarning("Archive verification explicitly bypassed for {Archive}", pathToArchive);
                return new(ResultEnum.Success, pathToArchive, "Using local file without verification");
            }

            var check = await CheckFileHashAsync(pathToArchive, expectedHash, cancellationToken).ConfigureAwait(false);
            if (check.IsSuccess)
            {
                return new(ResultEnum.Success, pathToArchive, "Using verified local file");
            }

            if (fixUrl.IsFile || check.ResultEnum != ResultEnum.HashError)
            {
                return new(check.ResultEnum, null, check.Message);
            }

            File.Delete(pathToArchive);
        }
        else if (fixUrl.IsFile)
        {
            return new(ResultEnum.NotFound, null, "The local archive does not exist.");
        }

        var download = await _filesDownloader.DownloadFileAsync(fixUrl, pathToArchive, cancellationToken).ConfigureAwait(false);
        if (!download.IsSuccess)
        {
            return new(download.ResultEnum, null, download.Message);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new(ResultEnum.Cancelled, null, "Verification cancelled");
        }

        if (!skipHashCheck)
        {
            var check = await CheckFileHashAsync(pathToArchive, expectedHash, cancellationToken).ConfigureAwait(false);
            if (!check.IsSuccess)
            {
                return new(check.ResultEnum, null, check.Message);
            }
        }
        else
        {
            _logger.LogWarning("Archive verification explicitly bypassed for {Archive}", pathToArchive);
        }

        if (!ClientProperties.IsDeveloperMode)
        {
            var result = await _apiInterface.AddNumberOfInstallsAsync(fixGuid, ClientProperties.CurrentVersion).ConfigureAwait(false);
            if (result.IsSuccess && _fixesProvider.Installs is not null)
            {
                _fixesProvider.Installs[fixGuid] = result.ResultObject.Value;
            }
        }

        return new(ResultEnum.Success, pathToArchive, "File downloaded successfully");
    }

    private static bool IsSha256Hash(string? hash) => hash is { Length: 64 } && hash.All(Uri.IsHexDigit);
    /// <summary>
    /// Get path to backup folder and create if it doesn't exist
    /// </summary>
    /// <param name="gameDir">Game install directory</param>
    /// <param name="fixName">Name of the fix</param>
    /// <returns>Absolute path to the backup folder</returns>
    private static string CreateAndGetBackupFolder(
        string gameDir,
        string fixName
        )
    {
        var backupFolderPath = Path.Combine(gameDir, ClientConstants.BackupFolder, fixName.Trim().Replace(' ', '_'));
        backupFolderPath = string.Concat(backupFolderPath.Split(Path.GetInvalidPathChars()));

        if (Directory.Exists(backupFolderPath))
        {
            Directory.Delete(backupFolderPath, true);
        }

        return backupFolderPath;
    }

    /// <summary>
    /// Copy or move files to the backup folder
    /// </summary>
    /// <param name="paths">List of files or paths to backup</param>
    /// <param name="gameDir">Game install folder</param>
    /// <param name="backupFolderPath">Absolute path to the backup folder</param>
    /// <param name="deleteOriginal">Will original file be deleted</param>
    private void BackupFiles(
        List<string>? paths,
        string gameDir,
        string backupFolderPath,
        bool deleteOriginal
        )
    {
        if (paths is null or [])
        {
            return;
        }

        _logger.LogInformation($"Backing up {paths.Count} paths.");

        for (var i = 0; i < paths.Count; i++)
        {
            _progressReport.OperationMessage = $"Backing up file {i + 1} of {paths.Count}.";

            var fullFilePath = ClientHelpers.GetFullPath(gameDir, paths[i]);

            if (fullFilePath.EndsWith('\\'))
            {
                if (!Directory.Exists(fullFilePath))
                {
                    continue;
                }

                var to = Path.Combine(backupFolderPath, paths[i]);

                if (deleteOriginal)
                {
                    DirectoryHelper.CopyDirectory(fullFilePath, to, true);
                    Directory.Delete(fullFilePath, true);
                }
                else
                {
                    DirectoryHelper.CopyDirectory(fullFilePath, to, true);
                }
            }
            else
            {
                if (!File.Exists(fullFilePath))
                {
                    continue;
                }

                var to = Path.Combine(backupFolderPath, paths[i]);

                var dir = Path.GetDirectoryName(to);

                ArgumentNullException.ThrowIfNull(dir);

                if (!Directory.Exists(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                if (deleteOriginal)
                {
                    File.Move(fullFilePath, to);
                }
                else
                {
                    File.Copy(fullFilePath, to);
                }
            }
        }

        _progressReport.OperationMessage = string.Empty;
    }

    /// <summary>
    /// Verifies the SHA-256 hash of a local archive without treating cancellation as success.
    /// </summary>
    /// <param name="filePath">Archive path.</param>
    /// <param name="hash">Expected SHA-256 hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success only after verification completes; otherwise a cancellation, hash, or IO error.</returns>
    public async Task<Result> CheckFileHashAsync(string filePath, string? hash, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSha256Hash(hash))
            {
                return new(ResultEnum.HashError, "The fix does not have a valid SHA-256 hash. Verification cannot proceed.");
            }

            _progressReport.OperationMessage = "Checking SHA-256 hash...";
            await using var stream = File.OpenRead(filePath);
            var fileHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            cancellationToken.ThrowIfCancellationRequested();
            return string.Equals(hash, fileHash, StringComparison.OrdinalIgnoreCase)
                ? new(ResultEnum.Success, string.Empty)
                : new(ResultEnum.HashError, "File's SHA-256 hash doesn't match the database");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hash checking cancelled");
            return new(ResultEnum.Cancelled, "Verification cancelled");
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, ex.Message);
        }
        finally
        {
            _progressReport.OperationMessage = string.Empty;
        }
    }
    /// <summary>
    /// Run or open whatever is in RunAfterInstall parameter
    /// </summary>
    /// <param name="gameInstallPath">Path to the game folder</param>
    /// <param name="runAfterInstall">File to open</param>
    private static void RunAfterInstall(
        string gameInstallPath,
        string? runAfterInstall
        )
    {
        if (string.IsNullOrEmpty(runAfterInstall))
        {
            return;
        }

        var path = Path.Combine(gameInstallPath, runAfterInstall);

        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
            WorkingDirectory = gameInstallPath
        });
    }

    private async Task PatchFilesAsync(List<string>? filesToPatch, string gameFolder, string backupFolder)
    {
        if (filesToPatch is null or [])
        {
            return;
        }

        _progressReport.OperationMessage = "Patching...";

        foreach (var file in filesToPatch)
        {
            var newFilePath = Path.Combine(gameFolder, file);
            var originalFilePath = Path.Combine(backupFolder, file);
            var patchFilePath = newFilePath + ".octodiff";

            if (!File.Exists(originalFilePath) ||
                !File.Exists(patchFilePath))
            {
                throw new InvalidDataException();
            }

            await using FileStream originalFile = new(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream patchFile = new(patchFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream newFile = new(newFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            DeltaApplier deltaApplier = new()
            {
                SkipHashCheck = false
            };

            var reporter = new OctodiffProgressReporter();
            reporter.NotifyProgressChanged += NotifyProgressChanged;

            await Task.Run(() =>
            {
                deltaApplier.Apply(
                    originalFile,
                    new BinaryDeltaReader(patchFile, reporter),
                    newFile);
            }).ConfigureAwait(false);
        }

        _progressReport.OperationMessage = string.Empty;

        void NotifyProgressChanged(float value)
        {
            IProgress<float> progress = _progressReport.Progress;

            progress.Report(value);
        }
    }
}

