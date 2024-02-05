using Common.Helpers;
using SharpCompress.Archives;
using System.Security.Cryptography;

namespace Common
{
    /// <summary>
    /// Class for working with archives
    /// </summary>
    public sealed class ArchiveTools (ProgressReport progressReport)
    {
        private readonly ProgressReport _progressReport = progressReport;

        /// <summary>
        /// Download ZIP
        /// </summary>
        /// <param name="url">Link to file download</param>
        /// <param name="filePath">Absolute path to destination file</param>
        /// <param name="hash">MD5 to check file against</param>
        /// <exception cref="Exception">Error while downloading file</exception>
        /// <exception cref="HashCheckFailedException">MD5 of the downloaded file doesn't match provided MD5</exception>
        public async Task CheckAndDownloadFileAsync(
            Uri url,
            string filePath,
            string? hash = null)
        {
            Logger.Info($"Started downloading file {url}");

            IProgress<float> progress = _progressReport.Progress;
            var tempFile = filePath + ".temp";

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            _progressReport.OperationMessage = "Downloading...";

            using HttpClient client = new();

            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                ThrowHelper.Exception("Error while downloading a file: " + response.StatusCode.ToString());
            }

            if (hash is not null)
            {
                if (response.Content.Headers.ContentMD5 is not null &&
                    !hash.Equals(Convert.ToHexString(response.Content.Headers.ContentMD5)))
                {
                    ThrowHelper.HashCheckFailedException("File hash doesn't match");
                }
            }

            await using var source = await response.Content.ReadAsStreamAsync();
            var contentLength = response.Content.Headers.ContentLength;

            FileStream file = new(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            if (!contentLength.HasValue)
            {
                await source.CopyToAsync(file);

                await file.DisposeAsync();
            }
            else
            {
                var buffer = new byte[81920];
                var totalBytesRead = 0f;
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer)) != 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;

                    var value = (totalBytesRead / (long)contentLength * 100);
                    progress.Report(value);
                }

                await file.DisposeAsync();

                File.Move(tempFile, filePath);
            }

            if (hash is not null)
            {
                using var md5 = MD5.Create();
                var stream = File.OpenRead(filePath);

                var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream));

                if (!hash.Equals(fileHash))
                {
                    await stream.DisposeAsync();

                    File.Delete(filePath);

                    ThrowHelper.HashCheckFailedException("File hash doesn't match");
                }
                
                await stream.DisposeAsync();
            }

            _progressReport.OperationMessage = string.Empty;
        }

        /// <summary>
        /// Unpack fix from zip archive
        /// </summary>
        /// <param name="pathToArchive">Absolute path to archive file</param>
        /// <param name="unpackTo">Directory to unpack archive to</param>
        /// <param name="variant">Fix variant</param>
        public async Task UnpackArchiveAsync(
            string pathToArchive,
            string unpackTo,
            string? variant)
        {
            IProgress<float> progress = _progressReport.Progress;
            _progressReport.OperationMessage = "Unpacking...";
            var subfolder = variant + "/";

            using var archive = ArchiveFactory.Open(pathToArchive);
            using var reader = archive.ExtractAllEntries();

            var entriesCount = variant is null
            ? archive.Entries.Count()
            : archive.Entries.Count(x => x.Key.StartsWith(subfolder));

            var entryNumber = 1f;

            await Task.Run(() =>
            {
                while (reader.MoveToNextEntry())
                {
                    var entry = reader.Entry;

                    if (variant is not null &&
                        !entry.Key.StartsWith(variant + "/"))
                    {
                        continue;
                    }

                    var fullName = variant is null
                        ? Path.Combine(unpackTo, entry.Key)
                        : Path.Combine(unpackTo, entry.Key.Replace(variant + "/", string.Empty));

                    if (!Directory.Exists(Path.GetDirectoryName(fullName)))
                    {
                        var dirName = Path.GetDirectoryName(fullName) ?? ThrowHelper.ArgumentNullException<string>(fullName);
                        Directory.CreateDirectory(dirName);
                    }

                    if (entry.IsDirectory)
                    {
                        //it's a directory
                        Directory.CreateDirectory(fullName);
                    }
                    else
                    {
                        using FileStream writableStream = File.OpenWrite(fullName);
                        reader.WriteEntryTo(writableStream);
                    }

                    var value = entryNumber / entriesCount * 100;
                    progress.Report(value);

                    entryNumber++;
                }
            }
            );

            _progressReport.OperationMessage = string.Empty;
        }

        /// <summary>
        /// Get list of files and new folders in the archive
        /// </summary>
        /// <param name="pathToArchive">Path to ZIP</param>
        /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
        /// <param name="unpackToPath">Full path</param>
        /// <param name="variant">Fix variant</param>
        /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
        public List<string> GetListOfFilesInArchive(
            string pathToArchive,
            string unpackToPath,
            string? fixInstallFolder,
            string? variant)
        {
            using var archive = ArchiveFactory.Open(pathToArchive);
            using var reader = archive.ExtractAllEntries();

            List<string> files = new(archive.Entries.Count() + 1);

            //if directory that the archive will be extracted to doesn't exist, add it to the list too
            if (!Directory.Exists(unpackToPath) &&
                fixInstallFolder is not null)
            {
                files.Add(fixInstallFolder + Path.DirectorySeparatorChar);
            }

            while (reader.MoveToNextEntry())
            {
                var entry = reader.Entry;

                var path = entry.Key;

                if (variant is not null)
                {
                    if (entry.Key.StartsWith(variant + '/'))
                    {
                        path = entry.Key.Replace(variant + '/', string.Empty);

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
                    fixInstallFolder ?? string.Empty,
                    path)
                    .Replace('/', Path.DirectorySeparatorChar);

                //if it's a file, add it to the list
                if (!entry.IsDirectory)
                {
                    files.Add(fullName);
                }
                //if it's a directory and it doesn't already exist, add it to the list
                else if (!Directory.Exists(Path.Combine(unpackToPath, path)))
                {
                    files.Add(fullName);
                }
            }

            return files;
        }
    }
}
