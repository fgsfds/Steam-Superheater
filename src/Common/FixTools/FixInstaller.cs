using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace Common.FixTools
{
    public class FixInstaller
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
            string backupFolder = fix.Name.Replace(' ', '_');

            string? zipName = null;
            string? zipFullPath = null;
            string? unpackToPath = null;
            List<string> filesInArchive = new();

            if (!string.IsNullOrEmpty(fix.Url))
            {
                zipName = Path.GetFileName(fix.Url);

                backupFolder = Path.GetFileNameWithoutExtension(zipName);

                zipFullPath = _configEntity.UseLocalRepo
                    ? Path.Combine(_configEntity.LocalRepoPath, "fixes", zipName)
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, zipName);

                unpackToPath = fix.InstallFolder is null
                    ? game.InstallDir
                    : Path.Combine(game.InstallDir, fix.InstallFolder) + Path.DirectorySeparatorChar;

                if (!File.Exists(zipFullPath))
                {
                    var url = fix.Url;

                    if (_configEntity.UseTestRepoBranch)
                    {
                        url = url.Replace("/master/", "/test/");
                    }

                    await FileTools.DownloadFileAsync(new Uri(url), zipFullPath);
                }

                filesInArchive = GetListOfFilesInArchive(zipFullPath, fix.InstallFolder, unpackToPath, variant);

                BackupFiles(filesInArchive, game.InstallDir, backupFolder, true, true);
            }

            BackupFiles(fix.FilesToDelete, game.InstallDir, backupFolder, true, true);

            BackupFiles(fix.FilesToBackup, game.InstallDir, backupFolder, false, false);

            if (zipFullPath is not null &&
                unpackToPath is not null)
            {
                await FileTools.UnpackZipAsync(zipFullPath, unpackToPath, variant);

                if (_configEntity.DeleteZipsAfterInstall &&
                    !_configEntity.UseLocalRepo)
                {
                    File.Delete(zipFullPath);
                }
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
        private void RunAfterInstall(string gameInstallPath, string runAfterInstall)
        {
            var path = Path.Combine(gameInstallPath, runAfterInstall);

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                WorkingDirectory = gameInstallPath
            });
        }

        /// <summary>
        /// Backup files
        /// </summary>
        /// <param name="files">List of files to backup</param>
        /// <param name="gameDir">Game install folder</param>
        /// <param name="backupFolder">Name of the backup folder</param>
        /// <param name="deleteOriginal">Will original file be deleted</param>
        private void BackupFiles(
            IEnumerable<string>? files,
            string gameDir,
            string backupFolder,
            bool deleteOriginal,
            bool deleteBackupFolder
            )
        {
            if (files is null ||
                !files.Any())
            {
                return;
            }

            var fixFolderPath = Path.Combine(gameDir, Consts.BackupFolder, backupFolder);
            fixFolderPath = string.Join(string.Empty, fixFolderPath.Split(Path.GetInvalidPathChars()));

            if (deleteBackupFolder && Directory.Exists(fixFolderPath))
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
        /// Get list of files and new folders in the archive
        /// </summary>
        /// <param name="zipPath">Path to ZIP</param>
        /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
        /// <param name="unpackToPath">Full path </param>
        /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
        private List<string> GetListOfFilesInArchive(string zipPath, string? fixInstallFolder, string unpackToPath, string? variant)
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
