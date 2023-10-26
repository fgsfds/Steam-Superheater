using Common.Helpers;
using System.IO.Compression;
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
        public static async Task CheckAndDownloadFileAsync(Uri url, string filePath, string? hash = null)
        {
            IProgress<float> progress = Progress;
            var tempFile = filePath + ".temp";

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    throw new ("Error while downloading a file: " + response.StatusCode.ToString());
                }

                if (hash is not null)
                {
                    if (response.Content.Headers.ContentMD5 is not null &&
                        !hash.Equals(Convert.ToHexString(response.Content.Headers.ContentMD5)))
                    {
                        throw new HashCheckFailedException("File hash doesn't match");
                    }
                }

                using var source = await response.Content.ReadAsStreamAsync();
                var contentLength = response.Content.Headers.ContentLength;

                using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

                if (Progress is null || !contentLength.HasValue)
                {
                    await source.CopyToAsync(file);
                }
                else
                {
                    var buffer = new byte[81920];
                    float totalBytesRead = 0f;
                    int bytesRead;

                    while ((bytesRead = await source.ReadAsync(buffer)) != 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalBytesRead += bytesRead;
                        progress.Report((float)(totalBytesRead / contentLength * 100));
                    }

                    await file.DisposeAsync();

                    File.Move(tempFile, filePath);
                }

                if (hash is not null)
                {
                    using (var md5 = MD5.Create())
                    {
                        using var stream = File.OpenRead(filePath);

                        var fileHash = Convert.ToHexString(md5.ComputeHash(stream));

                        if (!hash.Equals(fileHash))
                        {
                            await stream.DisposeAsync();

                            File.Delete(filePath);

                            throw new HashCheckFailedException("File hash doesn't match");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unpack fix from zip archive
        /// </summary>
        /// <param name="pathToZip">Absolute path to zip file</param>
        /// <param name="unpackTo">Directory to unpack zip to</param>
        /// <param name="variant">Fix variant</param>
        public static async Task UnpackZipAsync(string pathToZip, string unpackTo, string? variant)
        {
            IProgress<float> progress = Progress;

            using (var zip = ZipFile.OpenRead(pathToZip))
            {
                var count = variant is null
                    ? zip.Entries.Count
                    : zip.Entries.Where(x => x.FullName.StartsWith(variant)).Count();

                var i = 1f;

                await Task.Run(() =>
                {
                    foreach (var zipEntry in zip.Entries)
                    {
                        if (variant is not null &&
                        !zipEntry.FullName.StartsWith(variant))
                        {
                            continue;
                        }

                        var fullName = variant is null
                            ? Path.Combine(unpackTo, zipEntry.FullName)
                            : Path.Combine(unpackTo, zipEntry.FullName.Replace(variant + "/", string.Empty));

                        if (!Directory.Exists(Path.GetDirectoryName(fullName)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullName) ?? throw new Exception());
                        }

                        if (Path.GetFileName(fullName).Length == 0)
                        {
                            //it's a directory
                            Directory.CreateDirectory(fullName);
                        }
                        else
                        {
                            zipEntry.ExtractToFile(fullName, true);
                        }

                        progress.Report(i / count * 100);

                        i++;
                    }
                });
            }
        }
    }
}
