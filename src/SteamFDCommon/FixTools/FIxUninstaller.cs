using SteamFDCommon;
using SteamFDCommon.Entities;
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
            foreach (var file in fix.InstalledFix.FilesList)
            {
                var fullPath = Path.Combine(game.InstallDir, file);

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

            RestoreBackup(game.InstallDir, Path.GetFileNameWithoutExtension(fix.Url));

            var backupFolder = Path.Combine(game.InstallDir, Consts.BackupFolder);

            if (Directory.Exists(backupFolder) &&
                !Directory.GetFiles(backupFolder).Any() &&
                !Directory.GetDirectories(backupFolder).Any())
            {
                Directory.Delete(backupFolder);
            }
        }

        /// <summary>
        /// Restore backed up files
        /// </summary>
        /// <param name="gameDir">Game install folder</param>
        /// <param name="fixName">The name of the backup's fix</param>
        private static void RestoreBackup(string gameDir, string fixName)
        {
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
    }
}
