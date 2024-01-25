using Common.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using Octodiff.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixInstaller(
        ConfigProvider config,
        ArchiveTools archiveTools,
        ProgressReport progressReport
        )
    {
        private readonly ConfigEntity _configEntity = config.Config;
        private readonly ArchiveTools _archiveTools = archiveTools;
        private readonly ProgressReport _progressReport = progressReport;

        /// <summary>
        /// Install file fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        /// <param name="variant">Fix variant</param>
        /// <param name="skipMD5Check">Don't check file against fix's MD5 hash</param>
        /// <exception cref="Exception">Error while downloading file</exception>
        /// <exception cref="HashCheckFailedException">MD5 of the downloaded file doesn't match provided MD5</exception>
        public async Task<BaseInstalledFixEntity> InstallFixAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check)
        {
            FileInstalledFixEntity? installedSharedFix = null;

            if (fix.SharedFix is not null)
            {
                fix.SharedFix.InstallFolder = fix.SharedFixInstallFolder;

                installedSharedFix = (FileInstalledFixEntity)await InstallFixAsync(game, fix.SharedFix, variant, skipMD5Check);
            }

            var backupFolderPath = CreateAndGetBackupFolder(game.InstallDir, fix.Name);
            List<string> filesInArchive = [];

            if (fix.Url is not null)
            {
                var unpackToPath = fix.InstallFolder is null
                    ? game.InstallDir
                    : Path.Combine(game.InstallDir, fix.InstallFolder) + Path.DirectorySeparatorChar;

                var pathToArchive = _configEntity.UseLocalRepo
                    ? Path.Combine(_configEntity.LocalRepoPath, "fixes", Path.GetFileName(fix.Url))
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fix.Url));

                await CheckAndDownloadFileAsync(pathToArchive, fix.Url, skipMD5Check ? null : fix.MD5);

                filesInArchive = _archiveTools.GetListOfFilesInArchive(pathToArchive, unpackToPath, fix.InstallFolder, variant);

                BackupFiles(filesInArchive, game.InstallDir, backupFolderPath, true);

                await UnpackArchiveAsync(pathToArchive, unpackToPath, variant);
            }

            BackupFiles(fix.FilesToDelete, game.InstallDir, backupFolderPath, true);
            BackupFiles(fix.FilesToBackup, game.InstallDir, backupFolderPath, false);
            BackupFiles(fix.FilesToPatch, game.InstallDir, backupFolderPath, true);

            await PatchFilesAsync(fix.FilesToPatch, game.InstallDir, backupFolderPath);

            var dllOverrides = await ApplyWineDllOverrides(game.Id, fix.WineDllOverrides);

            RunAfterInstall(game.InstallDir, fix.RunAfterInstall);

            if (!Directory.Exists(backupFolderPath))
            {
                backupFolderPath = null;
            }

            FileInstalledFixEntity installedFix = new()
            {
                GameId = game.Id,
                Guid = fix.Guid,
                Version = fix.Version,
                BackupFolder = backupFolderPath is null ? null : new DirectoryInfo(backupFolderPath).Name,
                FilesList = filesInArchive,
                InstalledSharedFix = installedSharedFix,
                WineDllOverrides = dllOverrides
            };

            return installedFix;
        }

        /// <summary>
        /// Add dll overrides to the registry
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="dllList">List of DLLs to add</param>
        /// <returns>List of added lines</returns>
        private async Task<List<string>?> ApplyWineDllOverrides(
            int gameId,
            List<string>? dllList)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                dllList is null)
            {
                return null;
            }

            string file = @$"{Environment.GetEnvironmentVariable("HOME")}/.local/share/Steam/steamapps/compatdata/{gameId}/pfx/user.reg";

            var linesList = File.ReadAllLines(file).ToList();

            var startIndex = linesList.FindIndex(static x => x.Contains(@"[Software\\Wine\\DllOverrides]"));

            List<string> addedLines = [];

            foreach (var dll in dllList)
            {
                var line = @$"""{dll}""=""native,builtin""";

                addedLines.Add(line);
                linesList.Insert(startIndex + 1, line);
            }

            await File.WriteAllLinesAsync(file, linesList);

            return addedLines;
        }

        /// <summary>
        /// Check file's MD5 and download if MD5 is correct
        /// </summary>
        /// <param name="fixUrl">Url to the file</param>
        /// <param name="fixMD5">MD5 of the file</param>
        /// <exception cref="Exception">Error while downloading file</exception>
        /// <exception cref="HashCheckFailedException">MD5 of the downloaded file doesn't match provided MD5</exception>
        private Task CheckAndDownloadFileAsync(string zipFullPath, string fixUrl, string? fixMD5)
        {
            //checking md5 of the existing file
            if (File.Exists(zipFullPath))
            {
                Logger.Info($"Using local file {zipFullPath}");

                var result = CheckFileMD5(zipFullPath, fixMD5);

                if (!result)
                {
                    Logger.Info("MD5 of the local file doesn't match, removing it");
                    File.Delete(zipFullPath);
                }
            }

            if (File.Exists(zipFullPath))
            {
                return Task.CompletedTask;
            }

            Logger.Info($"Local file {zipFullPath} not found");

            var url = fixUrl;

            if (_configEntity.UseTestRepoBranch)
            {
                url = url.Replace("/master/", "/test/");
            }

            return _archiveTools.CheckAndDownloadFileAsync(new Uri(url), zipFullPath, fixMD5);

        }

        /// <summary>
        /// Get path to backup folder and create if it doesn't exist
        /// </summary>
        /// <param name="gameDir">Game install directory</param>
        /// <param name="fixName">Name of the fix</param>
        /// <returns>Absolute path to the backup folder</returns>
        private static string CreateAndGetBackupFolder(string gameDir, string fixName)
        {
            var backupFolderPath = Path.Combine(gameDir, Consts.BackupFolder, fixName.Replace(' ', '_'));
            backupFolderPath = string.Join(string.Empty, backupFolderPath.Split(Path.GetInvalidPathChars()));

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
        private static void BackupFiles(
            List<string>? files,
            string gameDir,
            string backupFolderPath,
            bool deleteOriginal)
        {
            if (files is null || files.Count == 0)
            {
                return;
            }

            foreach (var file in files)
            {
                var fullFilePath = Path.Combine(gameDir, file);

                if (!File.Exists(fullFilePath))
                {
                    continue;
                }

                var to = Path.Combine(backupFolderPath, file);

                var dir = Path.GetDirectoryName(to);

                if (dir is null)
                {
                    ThrowHelper.NullReferenceException(nameof(dir));
                }

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
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

        /// <summary>
        /// Check MD5 of the local file
        /// </summary>
        /// <param name="filePath">Full path to the file</param>
        /// <param name="fixMD5">MD5 that the file's hash will be compared to</param>
        /// <returns>true if check is passed</returns>
        private static bool CheckFileMD5(string filePath, string? fixMD5)
        {
            if (fixMD5 is null) { return true; }

            string? hash;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    hash = Convert.ToHexString(md5.ComputeHash(stream));
                }
            }

            return fixMD5.Equals(hash);
        }

        /// <summary>
        /// Backup files that will be replaced and unpack Zip
        /// </summary>
        /// <param name="archiveFullPath">Path to archive file</param>
        /// <param name="unpackToPath">Where to unpack archive</param>
        /// <param name="variant">Fix variant</param>
        private async Task UnpackArchiveAsync(
            string archiveFullPath,
            string unpackToPath,
            string? variant)
        {
            await _archiveTools.UnpackArchiveAsync(archiveFullPath, unpackToPath, variant);

            if (_configEntity.DeleteZipsAfterInstall &&
                !_configEntity.UseLocalRepo)
            {
                File.Delete(archiveFullPath);
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

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                WorkingDirectory = gameInstallPath
            });
        }

        private async Task PatchFilesAsync(List<string>? filesToPatch, string gameFolder, string backupFolder)
        {
            if (filesToPatch is null || filesToPatch.Count == 0)
            {
                return;
            }

            _progressReport.OperationMessage = "Patching...";

            foreach (var file in filesToPatch)
            {
                var newFilePath = Path.Combine(gameFolder, file);
                var originalFilePath = Path.Combine(backupFolder, file);
                var patchFilePath = newFilePath + ".octodiff";

                if (!File.Exists(originalFilePath)||
                    !File.Exists(patchFilePath))
                {
                    throw new Exception();
                }

                using FileStream originalFile = new(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using FileStream patchFile = new(patchFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using FileStream newFile = new(newFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

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
                });
            }

            _progressReport.OperationMessage = string.Empty;

            void NotifyProgressChanged(float value)
            {
                IProgress<float> progress = _progressReport.Progress;

                progress.Report(value);
            }
        }
    }
}
