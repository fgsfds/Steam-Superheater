using SteamFDCommon;
using SteamFDCommon.Config;
using SteamFDCommon.DI;
using SteamFDTCommon.Entities;
using System.Diagnostics;
using System.IO.Compression;

namespace SteamFDTCommon.FixTools
{
    public static class FixInstaller
    {
        private static readonly ConfigEntity _config = BindingsManager.Instance.GetInstance<ConfigProvider>().Config;

        /// <summary>
        /// Install fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public static async Task<InstalledFixEntity> InstallFix(GameEntity game, FixEntity fix, bool doBackup)
        {
            var url = fix.Url;

            if (!url.StartsWith("http"))
            {
                url = Consts.GitHubRepo + url;
            }

            var zipName = Path.GetFileName(url);

            var zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, zipName);

            var gameInstallPath = game.InstallDir;

            var unpackToPath = fix.InstallFolder is null
                                  ? gameInstallPath
                                  : Path.Combine(gameInstallPath, fix.InstallFolder);

            if (!File.Exists(zipPath))
            {
                await ZipTools.DownloadFileAsync(new Uri(url), zipPath);
            }

            var files = GetListOfFilesInArchive(zipPath, fix.InstallFolder, unpackToPath);

            var filesToDelete = GetListOfFilesToDelete(gameInstallPath, fix.FilesToDelete);

            if (doBackup)
            {
                BackupFiles(
                    files.Concat(filesToDelete).ToList(),
                    gameInstallPath,
                    Path.GetFileNameWithoutExtension(zipName)
                    );
            }

            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, unpackToPath, true));

            if (_config.DeleteZipsAfterInstall)
            {
                File.Delete(zipPath);
            }

            if (fix.RunAfterInstall is not null)
            {
                var path = Path.Combine(gameInstallPath, fix.RunAfterInstall);

                var currentDir = Directory.GetCurrentDirectory();

                Directory.SetCurrentDirectory(gameInstallPath);

                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo(path)
                };

                proc.Start();
                await proc.WaitForExitAsync();

                Directory.SetCurrentDirectory(currentDir);
            }

            InstalledFixEntity installedFix = new(game.Id, fix.Guid, fix.Version, files);

            return installedFix;
        }

        /// <summary>
        /// Get list of files that fill be deleted before the fix is installed
        /// </summary>
        /// <param name="gameInstallPath">Path to the game folder</param>
        /// <param name="filesToDelete">Files that need to be deleted, relative paths searated by ;</param>
        /// <returns>List of relative paths to deleted files</returns>
        private static List<string> GetListOfFilesToDelete(string gameInstallPath, string? filesToDelete)
        {
            List<string> result = new();

            if (filesToDelete is null)
            {
                return result;
            }

            var filesToDeleteSplit = filesToDelete.Split(";");

            foreach (var file in filesToDeleteSplit)
            {
                if (File.Exists(Path.Combine(gameInstallPath, file)))
                {
                    result.Add(file);
                }
            }

            return result;
        }

        /// <summary>
        /// Backup files
        /// </summary>
        /// <param name="files">List of files to backup</param>
        /// <param name="gameDir">Game install folder</param>
        /// <param name="backupFolder">Name of the backup folder</param>
        private static void BackupFiles(
            List<string> files,
            string gameDir,
            string backupFolder
            )
        {
            var fixFolderPath = Path.Combine(gameDir, Consts.BackupFolder, backupFolder);
            fixFolderPath = string.Join(string.Empty, fixFolderPath.Split(Path.GetInvalidPathChars()));

            if (Directory.Exists(fixFolderPath))
            {
                Directory.Delete(fixFolderPath, true);
            }

            foreach (var file in files)
            {
                var fullFilePath = Path.Combine(gameDir, file);

                if (File.Exists(fullFilePath))
                {
                    var from = fullFilePath;
                    var to = Path.Combine(fixFolderPath, file);

                    var dir = Path.GetDirectoryName(to);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.Move(from, to);
                }
            }
        }

        /// <summary>
        /// Get list of files and new folders in the archive
        /// </summary>
        /// <param name="zipPath">Path to ZIP</param>
        /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
        /// <param name="unpackToPath">Full path </param>
        /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
        private static List<string> GetListOfFilesInArchive(string zipPath, string? fixInstallFolder, string unpackToPath)
        {
            List<string> files = new();

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var fullName = Path.Combine(
                        fixInstallFolder is null ? string.Empty : fixInstallFolder,
                        entry.FullName)
                        .Replace("/", "\\");

                    //if it's a file, add it to the list
                    if (!fullName.EndsWith("\\"))
                    {
                        files.Add(fullName);
                    }
                    //if it's a directory and it doesn't already exist, add it to the list
                    else if (!Directory.Exists(Path.Combine(unpackToPath, entry.FullName)))
                    {
                        files.Add(fullName);
                    }
                }
            }

            return files;
        }
    }
}
