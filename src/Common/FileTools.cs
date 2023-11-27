using Common.Helpers;
using SharpCompress.Archives;
using System.Security.Cryptography;

namespace Common
{
    public static class FileTools
    {
        /// <summary>
        /// Operation progress
        /// </summary>
        public static readonly Progress<float> Progress = new();

        /// <summary>
        /// Download ZIP
        /// </summary>
        /// <param name="url">Link to file download</param>
        /// <param name="filePath">Absolute path to destination file</param>
        /// <param name="hash">MD5 to check file against</param>
        /// <exception cref="Exception">Error while downloading file</exception>
        /// <exception cref="HashCheckFailedException">MD5 of the downloaded file doesn't match provided MD5</exception>
        public static async Task CheckAndDownloadFileAsync(
            Uri url,
            string filePath,
            string? hash = null)
        {
            Logger.Info($"Started downloading file {url}");

            IProgress<float> progress = Progress;
            var tempFile = filePath + ".temp";

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            using (HttpClient client = new())
            {
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

                await using FileStream file = new(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                if (!contentLength.HasValue)
                {
                    await source.CopyToAsync(file);
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
                        progress.Report((totalBytesRead / (long)contentLength * 100));
                    }

                    await file.DisposeAsync();

                    File.Move(tempFile, filePath);
                }

                if (hash is not null)
                {
                    using (var md5 = MD5.Create())
                    {
                        await using var stream = File.OpenRead(filePath);

                        var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream));

                        if (!hash.Equals(fileHash))
                        {
                            await stream.DisposeAsync();

                            File.Delete(filePath);

                            ThrowHelper.HashCheckFailedException("File hash doesn't match");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unpack fix from zip archive
        /// </summary>
        /// <param name="pathToArchive">Absolute path to archive file</param>
        /// <param name="unpackTo">Directory to unpack archive to</param>
        /// <param name="variant">Fix variant</param>
        public static async Task UnpackArchiveAsync(
            string pathToArchive,
            string unpackTo,
            string? variant)
        {
            IProgress<float> progress = Progress;

            using (var archive = ArchiveFactory.Open(pathToArchive))
            {
                var sub = variant + "/";

                var count = variant is null
                    ? archive.Entries.Count()
                    : archive.Entries.Count(x => x.Key.StartsWith(sub));

                var i = 1f;

                foreach (var zipEntry in archive.Entries)
                {
                    if (variant is not null &&
                        !zipEntry.Key.StartsWith(variant + "/"))
                    {
                        continue;
                    }

                    var fullName = variant is null
                        ? Path.Combine(unpackTo, zipEntry.Key)
                        : Path.Combine(unpackTo, zipEntry.Key.Replace(variant + "/", string.Empty));

                    if (!Directory.Exists(Path.GetDirectoryName(fullName)))
                    {
                        var dirName = Path.GetDirectoryName(fullName) ?? ThrowHelper.ArgumentNullException<string>(fullName);
                        Directory.CreateDirectory(dirName);
                    }

                    if (zipEntry.IsDirectory)
                    {
                        //it's a directory
                        Directory.CreateDirectory(fullName);
                    }
                    else
                    {
                        await using FileStream target = new(fullName, FileMode.Create);
                        await zipEntry.OpenEntryStream().CopyToAsync(target);
                    }

                    progress.Report(i / count * 100);

                    i++;
                }
            }
        }

        /// <summary>
        /// Get list of files and new folders in the archive
        /// </summary>
        /// <param name="zipPath">Path to ZIP</param>
        /// <param name="fixInstallFolder">Folder to unpack the ZIP</param>
        /// <param name="unpackToPath">Full path</param>
        /// <param name="variant">Fix variant</param>
        /// <returns>List of files and folders (if aren't already exist) in the archive</returns>
        public static List<string> GetListOfFilesInArchive(
            string zipPath,
            string unpackToPath,
            string? fixInstallFolder,
            string? variant)
        {
            using var reader = ArchiveFactory.Open(zipPath);

            List<string> files = new(reader.Entries.Count() + 1);

            //if directory that the archive will be extracted to doesn't exist, add it to the list too
            if (!Directory.Exists(unpackToPath))
            {
                files.Add(unpackToPath);
            }

            foreach (var entry in reader.Entries)
            {
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
