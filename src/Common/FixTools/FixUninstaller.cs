﻿using Common.Entities;
using Common.Helpers;

namespace Common.FixTools
{
    public static class FixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Installed fix entity</param>
        /// <param name="fixEntity">Fix entity</param>
        public static void UninstallFix(GameEntity game, InstalledFixEntity fix, FixEntity fixEntity)
        {
            if (fix is null) throw new NullReferenceException(nameof(fix));

            DeleteFiles(game.InstallDir, fix.FilesList);

            RestoreBackup(game.InstallDir, fix, fixEntity.Url);

            DeleteBackupFolderIfEmpty(game.InstallDir);
        }

        /// <summary>
        /// Delete files from game install directory
        /// </summary>
        /// <param name="gameInstallDir">Game install folder</param>
        /// <param name="fixFiles">Files to delete</param>
        private static void DeleteFiles(string gameInstallDir, List<string>? fixFiles)
        {
            if (fixFiles is null) return;

            foreach (var file in fixFiles)
            {
                var fullPath = Path.Combine(gameInstallDir, file);

                if (!file.EndsWith(@"/") &&
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
        private static void RestoreBackup(
            string gameDir,
            InstalledFixEntity fix,
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
                if (fixUrl is not null)
                {
                    backupFolder = Path.Combine(gameDir, Consts.BackupFolder, Path.GetFileNameWithoutExtension(fixUrl));
                }
                else
                {
                    throw new BackwardsCompatibilityException("Can't get backup folder.");
                }
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
                !Directory.GetFiles(backupFolder).Any() &&
                !Directory.GetDirectories(backupFolder).Any())
            {
                Directory.Delete(backupFolder);
            }
        }
    }
}