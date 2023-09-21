using SteamFDCommon.Config;
using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using SteamFDCommon.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace SteamFDCommon.FixTools
{
    public static class FixInstaller
    {
        private static readonly ConfigEntity _config = BindingsManager.Instance.GetInstance<ConfigProvider>().Config;

        /// <summary>
        /// Install fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public static async Task<InstalledFixEntity> InstallFix(GameEntity game, FixEntity fix, string? variant)
        {
            var zipName = Path.GetFileName(fix.Url);

            var zipFullPath = _config.UseLocalRepo
                ? Path.Combine(_config.LocalRepoPath, "fixes", zipName)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, zipName);

            var unpackToPath = fix.InstallFolder is null
                                  ? game.InstallDir
                                  : Path.Combine(game.InstallDir, fix.InstallFolder) + "\\";

            if (!File.Exists(zipFullPath))
            {
                await FileTools.DownloadFileAsync(new Uri(fix.Url), zipFullPath);
            }

            var filesInArchive = GetListOfFilesInArchive(zipFullPath, fix.InstallFolder, unpackToPath, variant);

            var filesToDelete = GetListOfFilesToDelete(game.InstallDir, fix.FilesToDelete);

            BackupFiles(filesInArchive.Concat(filesToDelete), game.InstallDir, Path.GetFileNameWithoutExtension(zipName));

            await FileTools.UnpackZipAsync(zipFullPath, unpackToPath, variant);

            if (_config.DeleteZipsAfterInstall &&
                !_config.UseLocalRepo)
            {
                File.Delete(zipFullPath);
            }

            if (fix.RunAfterInstall is not null)
            {
                RunAfterInstall(game.InstallDir, fix.RunAfterInstall);
            }

            InstalledFixEntity installedFix = new(game.Id, fix.Guid, fix.Version, filesInArchive);

            return installedFix;
        }

        /// <summary>
        /// Run or open whatever is in RunAfterInstall parameter
        /// </summary>
        /// <param name="gameInstallPath">Path to the game folder</param>
        /// <param name="runAfterInstall">File to open</param>
        private static void RunAfterInstall(string gameInstallPath, string runAfterInstall)
        {
            var previousDir = Directory.GetCurrentDirectory();

            Directory.SetCurrentDirectory(gameInstallPath);

            try
            {
                var path = Path.Combine(gameInstallPath, runAfterInstall);

                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo(path)
                };

                proc.Start();
                //await proc.WaitForExitAsync();
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDir);
            }
        }

        /// <summary>
        /// Get list of files that fill be deleted before the fix is installed
        /// </summary>
        /// <param name="gameInstallPath">Path to the game folder</param>
        /// <param name="filesToDelete">Files that need to be deleted, relative paths separated by ;</param>
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
            IEnumerable<string> files,
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

                    if (dir is null)
                    {
                        throw new NullReferenceException(nameof(dir));
                    }

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
        private static List<string> GetListOfFilesInArchive(string zipPath, string? fixInstallFolder, string unpackToPath, string? variant)
        {
            List<string> files = new();

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string path = entry.FullName;

                    if (variant is not null)
                    {
                        if (entry.FullName.StartsWith(variant+"/"))
                        {
                            path = entry.FullName.Replace(variant + "/", string.Empty);

                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var fullName = Path.Combine(
                        fixInstallFolder is null ? string.Empty : fixInstallFolder,
                        path)
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
