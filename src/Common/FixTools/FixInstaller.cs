using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Common.FixTools
{
    public sealed class FixInstaller
    {
        private readonly ConfigEntity _configEntity;

        public FixInstaller(ConfigProvider config)
        {
            _configEntity = config.Config ?? throw new NullReferenceException(nameof(config));
        }

        /// <summary>
        /// Install fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public async Task<InstalledFixEntity> InstallFix(GameEntity game, FixEntity fix, string? variant)
        {
            string backupFolderPath = CreateAndGetBackupFolder(game, fix);

            BackupFiles(fix.FilesToDelete, game.InstallDir, backupFolderPath, true);

            BackupFiles(fix.FilesToBackup, game.InstallDir, backupFolderPath, false);

            var filesInArchive = await DownloadCheckAndUnpackZIP(fix, game.InstallDir, variant, backupFolderPath);

            RunAfterInstall(game.InstallDir, fix.RunAfterInstall);

            InstalledFixEntity installedFix = new(game.Id, fix.Guid, fix.Version, new DirectoryInfo(backupFolderPath).Name, filesInArchive);

            return installedFix;
        }

        private async Task<List<string>?> DownloadCheckAndUnpackZIP(
            FixEntity fix, 
            string gameDir,
            string? variant, 
            string backupFolderPath)
        {
            if (fix.Url is null)
            {
                return null;
            }

            string? zipFullPath = _configEntity.UseLocalRepo
                    ? Path.Combine(_configEntity.LocalRepoPath, "fixes", Path.GetFileName(fix.Url))
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fix.Url));

            await DownloadZip(fix.Url, zipFullPath);

            var md5CheckResult = CheckZipMD5(fix.MD5, zipFullPath);

            if (!md5CheckResult.Item1)
            {
                throw new Exception(md5CheckResult.Item2);
            }

            var filesInArchive = await BackupFilesAndUnpackZIP(gameDir, fix.InstallFolder, backupFolderPath, zipFullPath, variant);

            return filesInArchive;
        }

        /// <summary>
        /// Get path to backup folder and create if it doesn't exist
        /// </summary>
        /// <param name="game">Game Entity</param>
        /// <param name="fix">Fix Entity</param>
        /// <returns>Absolute path to the backup folder</returns>
        private static string CreateAndGetBackupFolder(GameEntity game, FixEntity fix)
        {
            var backupFolderPath = Path.Combine(game.InstallDir, Consts.BackupFolder, fix.Name.Replace(' ', '_'));
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
        /// <param name="deleteOriginal">Will original file be deleted</param>
        private static void BackupFiles(
            IEnumerable<string>? files,
            string gameDir,
            string backupFolderPath,
            bool deleteOriginal
            )
        {
            if (files is null || !files.Any())
            {
                return;
            }

            foreach (var file in files)
            {
                var fullFilePath = Path.Combine(gameDir, file);

                if (File.Exists(fullFilePath))
                {
                    var from = fullFilePath;
                    var to = Path.Combine(backupFolderPath, file);

                    var dir = Path.GetDirectoryName(to);

                    if (dir is null)
                    {
                        throw new NullReferenceException(nameof(dir));
                    }

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (deleteOriginal)
                    {
                        File.Move(from, to);
                    }
                    else
                    {
                        File.Copy(from, to);
                    }
                }
            }
        }

        /// <summary>
        /// Download and unpack ZIP if URL is not null
        /// </summary>
        /// <param name="fixUrl">URL to fix zip</param>
        /// <param name="zipFullPath">Fix install folder</param>
        /// <returns></returns>
        private async Task DownloadZip(string fixUrl, string zipFullPath)
        {
            if (!File.Exists(zipFullPath))
            {
                var url = fixUrl;

                if (_configEntity.UseTestRepoBranch)
                {
                    url = url.Replace("/master/", "/test/");
                }

                await FileTools.DownloadFileAsync(new Uri(url), zipFullPath);
            }
        }

        private static Tuple<bool, string> CheckZipMD5(string? fixMD5, string zipFullPath)
        {
            if (fixMD5 is null)
            {
                return new(true, string.Empty);
            }
            string? hash;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(zipFullPath))
                {
                    hash = Convert.ToHexString(md5.ComputeHash(stream));
                }
            }

            if (!fixMD5.Equals(hash))
            {
                File.Delete(zipFullPath);
                return new(false, "MD5 of the ZIP archive doesn't match the database. Installation is prohibited.");
            }

            return new(true, string.Empty);
        }

        private async Task<List<string>> BackupFilesAndUnpackZIP(
            string gameDir, 
            string? fixInstallFolder, 
            string backupFolderPath,
            string zipFullPath,
            string? variant)
        {
            var unpackToPath = fixInstallFolder is null
                ? gameDir
                : Path.Combine(gameDir, fixInstallFolder) + Path.DirectorySeparatorChar;

            var filesInArchive = GetListOfFilesInArchive(zipFullPath, fixInstallFolder, unpackToPath, variant);

            BackupFiles(filesInArchive, gameDir, backupFolderPath, true);

            await FileTools.UnpackZipAsync(zipFullPath, unpackToPath, variant);

            if (_configEntity.DeleteZipsAfterInstall &&
                !_configEntity.UseLocalRepo)
            {
                File.Delete(zipFullPath);
            }

            return filesInArchive;
        }

        /// <summary>
        /// Run or open whatever is in RunAfterInstall parameter
        /// </summary>
        /// <param name="gameInstallPath">Path to the game folder</param>
        /// <param name="runAfterInstall">File to open</param>
        private static void RunAfterInstall(string gameInstallPath, string? runAfterInstall)
        {
            if (runAfterInstall is null)
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

        /// <summary>
        /// Get list of files and new folders in the archive
        /// </summary>
        /// <param name="zipPath">Path to ZIP</param>
        /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
        /// <param name="unpackToPath">Full path </param>
        /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
        private static List<string> GetListOfFilesInArchive(
            string zipPath,
            string? fixInstallFolder,
            string unpackToPath,
            string? variant)
        {
            List<string> files = new();

            //if directory that the archive will be extracted to doesn't exist, add it to the list too
            if (!Directory.Exists(unpackToPath))
            {
                files.Add(unpackToPath);
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string path = entry.FullName;

                    if (variant is not null)
                    {
                        if (entry.FullName.StartsWith(variant + "/"))
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
                        .Replace('/', Path.DirectorySeparatorChar);

                    //if it's a file, add it to the list
                    if (!fullName.EndsWith("\\") &&
                        !fullName.EndsWith("/"))
                    {
                        files.Add(fullName);
                    }
                    //if it's a directory and it doesn't already exist, add it to the list
                    else if (!Directory.Exists(Path.Combine(unpackToPath, path)))
                    {
                        files.Add(fullName);
                    }
                }
            }

            return files;
        }
    }
}
