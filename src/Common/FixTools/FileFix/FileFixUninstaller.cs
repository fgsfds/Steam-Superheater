using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public void UninstallFix(GameEntity game, BaseInstalledFixEntity installedFix, string? fixUrl)
        {
            installedFix.ThrowIfNotType<FileInstalledFixEntity>(out var installedFileFix);

            if (installedFileFix.InstalledSharedFix is not null)
            {
                UninstallFix(game, installedFileFix.InstalledSharedFix, null);
            }

            DeleteFiles(game.InstallDir, installedFileFix.FilesList);

            RestoreBackup(game.InstallDir, installedFileFix, fixUrl);

            DeleteBackupFolderIfEmpty(game.InstallDir);
        }

        /// <summary>
        /// Delete files from game install directory
        /// </summary>
        /// <param name="gameInstallDir">Game install folder</param>
        /// <param name="fixFiles">Files to delete</param>
        private static void DeleteFiles(string gameInstallDir, List<string>? fixFiles)
        {
            if (fixFiles is null)
            {
                return;
            }

            //checking if files can be opened before deleting them
            foreach (var file in fixFiles)
            {
                var fullPath = Path.Combine(gameInstallDir, file);

                if (!file.EndsWith('/') &&
                    File.Exists(fullPath))
                {
                    var stream = File.Open(fullPath, FileMode.Open);
                    stream.Dispose();
                }
            }

            foreach (var file in fixFiles)
            {
                var fullPath = Path.Combine(gameInstallDir, file);

                if (!file.EndsWith('/') &&
                    File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
            }
        }

        /// <summary>
        /// Restore backed up files
        /// </summary>
        /// <param name="gameDir">Game install folder</param>
        /// <param name="fix">Installed fix</param>
        /// <param name="fixUrl">Url to fix file</param>
        private static void RestoreBackup(
            string gameDir,
            FileInstalledFixEntity fix,
            string? fixUrl)
        {
            string backupFolder;

            if (fix.BackupFolder is not null)
            {
                backupFolder = Path.Combine(gameDir, Consts.BackupFolder, fix.BackupFolder);
            }
            //TODO: Added for backwards compatibility, need to remove some time later
            else
            {
                if (fixUrl is null)
                {
                    ThrowHelper.BackwardsCompatibilityException("Can't get backup folder.");
                }

                backupFolder = Path.Combine(gameDir, Consts.BackupFolder, Path.GetFileNameWithoutExtension(fixUrl));
            }

            if (!Directory.Exists(backupFolder))
            {
                return;
            }

            var files = Directory.GetFiles(backupFolder, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(backupFolder, file);

                var pathTo = Path.Combine(gameDir, relativePath);

                File.Move(file, pathTo, true);
            }

            Directory.Delete(backupFolder, true);
        }

        /// <summary>
        /// Delete backup folder if it's empty
        /// </summary>
        /// <param name="gameInstallDir">Game install folder</param>
        private static void DeleteBackupFolderIfEmpty(string gameInstallDir)
        {
            var backupFolder = Path.Combine(gameInstallDir, Consts.BackupFolder);

            if (Directory.Exists(backupFolder) &&
                Directory.GetFiles(backupFolder).Length == 0 &&
                Directory.GetDirectories(backupFolder).Length == 0)
            {
                Directory.Delete(backupFolder);
            }
        }
    }
}
