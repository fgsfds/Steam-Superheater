using Api.Common.Interface;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Octodiff.Core;
using System.Diagnostics;
using System.Security.Cryptography;

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
    /// <param name="skipMD5Check">Don't check file against fix's MD5 hash</param>
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

        var sharedFixInstallResult = await InstallSharedFixAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

        if (!sharedFixInstallResult.IsSuccess)
        {
            return sharedFixInstallResult;
        }

        var backupFolderPath = CreateAndGetBackupFolder(game.InstallDir, fix.Name);

        BackupFiles(fix.FilesToDelete, game.InstallDir, backupFolderPath, true);
        BackupFiles(fix.FilesToBackup, game.InstallDir, backupFolderPath, false);
        BackupFiles(fix.FilesToPatch, game.InstallDir, backupFolderPath, true);

        var filesUnpackResult = await DownloadAndUnpackArchiveAsync(game, fix, variant, skipMD5Check, backupFolderPath, cancellationToken).ConfigureAwait(false);

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
            FilesList = filesUnpackResult.ResultObject
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

    private async Task<Result<Dictionary<string, long?>?>> DownloadAndUnpackArchiveAsync(
        GameEntity game,
        FileFixEntity fix,
        string? variant,
        bool skipMD5Check,
        string backupFolderPath,
        CancellationToken cancellationToken
        )
    {
        if (fix.Url is null)
        {
            return new(ResultEnum.Success, null, "Fix doesn't have downloadable file");
        }

        var unpackToPath = ClientHelpers.GetFullPath(game.InstallDir, fix.InstallFolder);

        var fileUri = new Uri(fix.Url);

        var fileDownloadResult = await CheckAndDownloadFileAsync(fileUri, fix.Guid, skipMD5Check ? null : fix.MD5, cancellationToken).ConfigureAwait(false);

        if (!fileDownloadResult.IsSuccess)
        {
            return new(fileDownloadResult.ResultEnum, null, fileDownloadResult.Message);
        }

        var filesInArchive = _archiveTools.GetListOfFilesInArchive(fileDownloadResult.ResultObject, unpackToPath, fix.InstallFolder, variant);

        BackupFiles([.. filesInArchive.Keys], game.InstallDir, backupFolderPath, true);

        await _archiveTools.UnpackArchiveAsync(fileDownloadResult.ResultObject, unpackToPath, variant).ConfigureAwait(false);

        if (_configEntity.DeleteZipsAfterInstall &&
            !_configEntity.UseLocalApiAndRepo &&
            fileDownloadResult.ResultObject?.StartsWith(ClientProperties.WorkingFolder) == true)
        {
            File.Delete(fileDownloadResult.ResultObject);
        }

        return new(ResultEnum.Success, filesInArchive, string.Empty);
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

    /// <summary>
    /// Check file's MD5 and download if MD5 is correct
    /// </summary>
    /// <param name="fixUrl">Url to the file</param>
    /// <param name="fixGuid">Fix GUID</param>
    /// <param name="fixMD5">MD5 of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="Exception">Error while downloading file</exception>
    private async Task<Result<string>> CheckAndDownloadFileAsync(
        Uri fixUrl,
        Guid fixGuid,
        string? fixMD5,
        CancellationToken cancellationToken
        )
    {
        string pathToArchive;

        if (fixUrl.IsFile && File.Exists(fixUrl.LocalPath))
        {
            return new(ResultEnum.Success, fixUrl.LocalPath, "Using local file");
        }
        else if (fixUrl.IsAbsoluteUri)
        {
            pathToArchive = _configEntity.UseLocalApiAndRepo
                ? Path.Combine(_configEntity.LocalRepoPath, "fixes", Path.GetFileName(fixUrl.LocalPath))
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fixUrl.LocalPath));
        }
        else
        {
            return ThrowHelper.ThrowArgumentOutOfRangeException<Result<string>>(nameof(fixUrl));
        }


        //checking md5 of the existing file
        if (File.Exists(pathToArchive))
        {
            _logger.LogInformation($"Checking local file {pathToArchive}");

            if (new FileInfo(pathToArchive).Length > 1e+9)
            {
                _progressReport.OperationMessage = "Checking hash... This may take a while. Press Cancel to skip.";
            }
            else
            {
                _progressReport.OperationMessage = "Checking hash...";
            }

            try
            {
                var fileCheckResult = await CheckFileMD5Async(pathToArchive, fixMD5, cancellationToken).ConfigureAwait(false);

                if (!fileCheckResult)
                {
                    _logger.LogInformation("MD5 of the local file doesn't match, removing it");
                    File.Delete(pathToArchive);
                }
                else
                {
                    _progressReport.OperationMessage = string.Empty;
                    return new(ResultEnum.Success, pathToArchive, "Using local file");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Hash checking cancelled");
                _progressReport.OperationMessage = string.Empty;
                return new(ResultEnum.Success, pathToArchive, "Using local file");
            }
            finally
            {
                _progressReport.OperationMessage = string.Empty;
            }
        }

        _logger.LogInformation($"Local file {pathToArchive} not found");

        var fileDownloadResult = await _filesDownloader.CheckAndDownloadFileAsync(fixUrl, pathToArchive, cancellationToken).ConfigureAwait(false);

        if (!fileDownloadResult.IsSuccess)
        {
            return new(fileDownloadResult.ResultEnum, null, fileDownloadResult.Message);
        }

        if (fixMD5 is not null)
        {
            var hashCheckResult = await _filesDownloader.CheckFileHashAsync(pathToArchive, fixMD5, cancellationToken).ConfigureAwait(false);

            if (!hashCheckResult.IsSuccess)
            {
                return new(hashCheckResult.ResultEnum, null, fileDownloadResult.Message);
            }
        }

        //Increasing downloads count
        if (!ClientProperties.IsDeveloperMode)
        {
            var result = await _apiInterface.AddNumberOfInstallsAsync(
                fixGuid,
                ClientProperties.CurrentVersion).ConfigureAwait(false);

            if (result.IsSuccess && _fixesProvider.Installs is not null)
            {
                _fixesProvider.Installs[fixGuid] = result.ResultObject.Value;
            }
        }

        return new(ResultEnum.Success, pathToArchive, "File downloaded successfully");
    }

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
        var backupFolderPath = Path.Combine(gameDir, Consts.BackupFolder, fixName.Trim().Replace(' ', '_'));
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
    /// <param name="files">List of files to backup</param>
    /// <param name="gameDir">Game install folder</param>
    /// <param name="backupFolderPath">Absolute path to the backup folder</param>
    /// <param name="deleteOriginal">Will original file be deleted</param>
    private void BackupFiles(
        List<string>? files,
        string gameDir,
        string backupFolderPath,
        bool deleteOriginal
        )
    {
        if (files is null or [])
        {
            return;
        }

        _logger.LogInformation($"Backing up {files.Count} files.");

        for (var i = 0; i < files.Count; i++)
        {
            _progressReport.OperationMessage = $"Backing up file {i+1} of {files.Count}.";

            var fullFilePath = ClientHelpers.GetFullPath(gameDir, files[i]);

            if (!File.Exists(fullFilePath))
            {
                continue;
            }

            var to = Path.Combine(backupFolderPath, files[i]);

            var dir = Path.GetDirectoryName(to);

            Guard.IsNotNull(dir);

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

        _progressReport.OperationMessage = string.Empty;
    }

    /// <summary>
    /// Check MD5 of the local file
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="fixMD5">MD5 that the file's hash will be compared to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>true if check is passed</returns>
    private static async Task<bool> CheckFileMD5Async(
        string filePath,
        string? fixMD5,
        CancellationToken cancellationToken
        )
    {
        if (fixMD5 is null)
        {
            return true;
        }

        string? hashStr;

        using (var md5 = MD5.Create())
        {
            await using var stream = File.OpenRead(filePath);

            var hash = await md5.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
            hashStr = Convert.ToHexString(hash);
        }

        return fixMD5.Equals(hashStr, StringComparison.OrdinalIgnoreCase);
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
                ThrowHelper.ThrowInvalidDataException();
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

