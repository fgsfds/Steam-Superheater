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
    public Task<Result<BaseInstalledFixEntity>> InstallFixAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken)
        => ExecuteAsync(game, fix, variant, skipMD5Check, cancellationToken, false, null);

    internal async Task<Result<BaseInstalledFixEntity>> ExecuteAsync(
        GameEntity game, FileFixEntity fix, string? variant, bool skipHashCheck, CancellationToken cancellationToken,
        bool update, Func<BaseInstalledFixEntity, Result>? persist)
    {
        var staging = Path.Combine(Path.GetTempPath(), "superheater-stage-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (cancellationToken.IsCancellationRequested) { return new(ResultEnum.Cancelled, null, "Installation cancelled"); }
            using var ownership = await FileInstallTransaction.AcquireAsync(game.InstallDir, cancellationToken).ConfigureAwait(false);
            FileInstallTransaction.Recover(game.InstallDir, true);
            var prepared = await PrepareTreeAsync(game, fix, variant, skipHashCheck, staging, new HashSet<Guid>(), cancellationToken).ConfigureAwait(false);
            if (!prepared.IsSuccess) { return new(prepared.ResultEnum, null, prepared.Message); }
            cancellationToken.ThrowIfCancellationRequested();
            var paths = GetAffectedPaths(game, prepared.ResultObject).ToList();
            paths.Add(Path.Combine(game.InstallDir, ClientConstants.BackupFolder));
            if (fix.InstalledFix is FileInstalledFixEntity installed) { paths.AddRange(GetInstalledPaths(game, installed)); }
            FileInstalledFixEntity result;
            using (var transaction = FileInstallTransaction.Begin(game.InstallDir, fix.Guid, paths))
            {
                if (update)
                {
                    ArgumentNullException.ThrowIfNull(fix.InstalledFix);
                    new FileFixUninstaller().UninstallFix(game, fix.InstalledFix);
                }
                result = await ApplyPreparedAsync(game, prepared.ResultObject, cancellationToken).ConfigureAwait(false);
                if (persist is not null)
                {
                    var saved = persist(result);
                    if (!saved.IsSuccess) { return new(saved.ResultEnum, null, saved.Message); }
                }
                transaction.Commit();
            }
            try { CleanupDownloads(prepared.ResultObject); RunPostInstallActions(game, prepared.ResultObject); }
            catch (Exception ex) { _logger.LogWarning(ex, "Fix installed, but a post-install action could not be started."); }
            return new(ResultEnum.Success, result, "Fix installed successfully.");
        }
        catch (OperationCanceledException)
        {
            return new(ResultEnum.Cancelled, null, "Installation cancelled; previous files restored.");
        }
        finally
        {
            _progressReport.OperationMessage = string.Empty;
            if (Directory.Exists(staging)) { Directory.Delete(staging, true); }
        }
    }

    private void CleanupDownloads(PreparedFix prepared)
    {
        if (prepared.Shared is not null) { CleanupDownloads(prepared.Shared); }
        if (_configEntity.DeleteZipsAfterInstall && !_configEntity.UseLocalApiAndRepo && prepared.ArchivePath is not null &&
            prepared.Fix.Url is not null && !new Uri(prepared.Fix.Url).IsFile &&
            Path.GetDirectoryName(prepared.ArchivePath) == Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory))
        {
            File.Delete(prepared.ArchivePath);
        }
    }

    private static void RunPostInstallActions(GameEntity game, PreparedFix prepared)
    {
        if (prepared.Shared is not null) { RunPostInstallActions(game, prepared.Shared); }
        RunAfterInstall(game.InstallDir, prepared.Fix.RunAfterInstall);
    }

    private sealed record PreparedFix(FileFixEntity Fix, string? Stage, Dictionary<string, long?>? Files, PreparedFix? Shared, string? Registry, string? ArchivePath);

    private async Task<Result<PreparedFix>> PrepareTreeAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipHashCheck, string staging, HashSet<Guid> visited, CancellationToken cancellationToken)
    {
        if (!visited.Add(fix.Guid)) { throw new InvalidDataException("Cyclic shared-fix dependency."); }
        cancellationToken.ThrowIfCancellationRequested();
        string? registry = null;
        if (fix.WineDllOverrides is not null && OperatingSystem.IsLinux())
        {
            registry = ProtonTools.FindUserRegistry(game.InstallDir, game.Id);
            if (registry is null) { return new(ResultEnum.NotFound, null, "Run the game once to create its Proton prefix."); }
        }
        Dictionary<string, long?>? files = null;
        string? stage = null;
        string? archivePath = null;
        if (fix.Url is not null)
        {
            var download = await CheckAndDownloadFileAsync(new Uri(fix.Url), fix.Guid, fix.Sha256, skipHashCheck, cancellationToken).ConfigureAwait(false);
            if (!download.IsSuccess) { return new(download.ResultEnum, null, download.Message); }
            archivePath = download.ResultObject;
            files = _archiveTools.GetListOfFilesInArchive(download.ResultObject, ClientHelpers.GetFullPath(game.InstallDir, fix.InstallFolder), fix.InstallFolder, variant);
            stage = Path.Combine(staging, fix.Guid.ToString("N"));
            await _archiveTools.UnpackArchiveAsync(download.ResultObject, stage, variant).ConfigureAwait(false);
        }
        PreparedFix? shared = null;
        if (fix.SharedFix is not null)
        {
            var sharedDefinition = fix.SharedFix.Clone();
            sharedDefinition.InstallFolder = fix.SharedFixInstallFolder;
            var sharedResult = await PrepareTreeAsync(game, sharedDefinition, variant, skipHashCheck, staging, visited, cancellationToken).ConfigureAwait(false);
            if (!sharedResult.IsSuccess) { return sharedResult; }
            shared = sharedResult.ResultObject;
        }
        _ = visited.Remove(fix.Guid);
        return new(ResultEnum.Success, new PreparedFix(fix, stage, files, shared, registry, archivePath), string.Empty);
    }

    private static IEnumerable<string> GetAffectedPaths(GameEntity game, PreparedFix prepared)
    {
        var fix = prepared.Fix;
        foreach (var path in (fix.FilesToDelete ?? []).Concat(fix.FilesToBackup ?? []).Concat(fix.FilesToPatch ?? []).Concat(prepared.Files?.Keys.AsEnumerable() ?? []))
        {
            yield return ClientHelpers.GetFullPath(game.InstallDir, path);
        }
        if (prepared.Registry is not null) { yield return prepared.Registry; }
        if (prepared.Shared is not null) { foreach (var path in GetAffectedPaths(game, prepared.Shared)) { yield return path; } }
    }

    private static IEnumerable<string> GetInstalledPaths(GameEntity game, FileInstalledFixEntity installed)
    {
        foreach (var path in installed.FilesList?.Keys.AsEnumerable() ?? []) { yield return ClientHelpers.GetFullPath(game.InstallDir, path); }
        if (installed.BackupFolder is not null)
        {
            var backup = Path.Combine(game.InstallDir, ClientConstants.BackupFolder, installed.BackupFolder);
            if (Directory.Exists(backup))
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(backup))
                {
                    yield return Path.Combine(game.InstallDir, Path.GetFileName(entry));
                }
            }
        }
        var registry = installed.WineRegistryPath ?? (installed.WineDllOverrides is null ? null : ProtonTools.FindUserRegistry(game.InstallDir, game.Id));
        if (registry is not null) { yield return registry; }
        if (installed.InstalledSharedFix is not null) { foreach (var path in GetInstalledPaths(game, installed.InstalledSharedFix)) { yield return path; } }
    }

    private async Task<FileInstalledFixEntity> ApplyPreparedAsync(GameEntity game, PreparedFix prepared, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var shared = prepared.Shared is null ? null : await ApplyPreparedAsync(game, prepared.Shared, cancellationToken).ConfigureAwait(false);
        var fix = prepared.Fix;
        if (prepared.Stage is not null && prepared.Files is not null)
        {
            var destination = ClientHelpers.GetFullPath(game.InstallDir, fix.InstallFolder);
            var refreshed = new Dictionary<string, long?>();
            if (fix.InstallFolder is not null && !Directory.Exists(destination))
            {
                refreshed.TryAdd(fix.InstallFolder + Path.DirectorySeparatorChar, null);
            }
            foreach (var pair in prepared.Files) { refreshed.TryAdd(pair.Key, pair.Value); }
            foreach (var directory in Directory.GetDirectories(prepared.Stage, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(prepared.Stage, directory);
                if (!Directory.Exists(Path.Combine(destination, relative)))
                {
                    refreshed.TryAdd(Path.Combine(fix.InstallFolder ?? string.Empty, relative) + Path.DirectorySeparatorChar, null);
                }
            }
            prepared = prepared with { Files = refreshed };
        }
        var backup = CreateAndGetBackupFolder(game.InstallDir, fix.Guid.ToString("N"));
        BackupFiles(fix.FilesToDelete, game.InstallDir, backup, true);
        BackupFiles(fix.FilesToBackup, game.InstallDir, backup, false);
        BackupFiles(fix.FilesToPatch, game.InstallDir, backup, true);
        if (prepared.Stage is not null)
        {
            BackupFiles([.. prepared.Files!.Keys], game.InstallDir, backup, true);
            FileInstallTransaction.CopyPath(prepared.Stage, ClientHelpers.GetFullPath(game.InstallDir, fix.InstallFolder));
        }
        cancellationToken.ThrowIfCancellationRequested();
        await PatchFilesAsync(fix.FilesToPatch, game.InstallDir, backup).ConfigureAwait(false);
        var overrides = prepared.Registry is null ? null : await ProtonTools.AddOverridesAsync(prepared.Registry, fix.WineDllOverrides!).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return new FileInstalledFixEntity
        {
            GameId = game.Id, Guid = fix.Guid, Version = fix.Version, BuildId = game.BuildId,
            BackupFolder = Directory.Exists(backup) ? new DirectoryInfo(backup).Name : null,
            FilesList = prepared.Files, InstalledSharedFix = shared, WineDllOverrides = overrides, WineRegistryPath = prepared.Registry
        };
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
            throw new IOException("A previous backup exists for this fix. Uninstall it or recover the interrupted operation before retrying.");
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
            if ((Path.IsPathRooted(paths[i]) || paths[i].StartsWith("{documents}", StringComparison.OrdinalIgnoreCase)) && File.Exists(fullFilePath))
            {
                throw new IOException("Replacing existing files outside the game folder requires an explicit backup mapping and is not supported.");
            }

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
