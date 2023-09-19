using SteamFDCommon.Entities;
using SteamFDCommon.Helpers;
using System.IO;

namespace SteamFDCommon.FixTools
{
    public static class FixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public static void UninstallFix(GameEntity game, FixEntity fix)
        {
            if (fix.InstalledFix is null)
            {
                throw new NullReferenceException(nameof(fix.InstalledFix));
            }

            DeleteFiles(game.InstallDir, fix.InstalledFix.FilesList);

            RestoreBackup(game.InstallDir, fix);

            DeleteBackupFolderIfEmpty(game.InstallDir);
        }

        /// <summary>
        /// Delete files from game install directory
        /// </summary>
        /// <param name="gameInstallDir">Game install folder</param>
        /// <param name="fixFiles">Files to delete</param>
        private static void DeleteFiles(string gameInstallDir, List<string> fixFiles)
        {
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
        private static void RestoreBackup(string gameDir, FixEntity fix)
        {
            var fixName = Path.GetFileNameWithoutExtension(fix.Url);

            var fixFolderPath = Path.Combine(gameDir, Consts.BackupFolder, fixName);
            fixFolderPath = string.Join(string.Empty, fixFolderPath.Split(Path.GetInvalidPathChars()));

            if (!Directory.Exists(fixFolderPath))
            {
                return;
            }

            var files = Directory.GetFiles(fixFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(fixFolderPath, file);

                var pathTo = Path.Combine(gameDir, relativePath);

                File.Move(file, pathTo, true);
            }

            Directory.Delete(fixFolderPath, true);
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
