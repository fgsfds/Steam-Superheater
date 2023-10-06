using System.IO.Compression;

namespace Common
{
    public sealed class FileTools
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
        public static async Task DownloadFileAsync(Uri url, string filePath)
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

                using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    file.Dispose();
                    File.Delete(tempFile);
                    throw new Exception("Error while downloading a file: " + response.StatusCode.ToString());
                }

                using var source = await response.Content.ReadAsStreamAsync();
                var contentLength = response.Content.Headers.ContentLength;

                if (Progress is null || !contentLength.HasValue)
                {
                    await source.CopyToAsync(file);

                    return;
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
                }
            }

            File.Move(tempFile, filePath);
            return;
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
                            Directory.CreateDirectory(Path.GetDirectoryName(fullName));
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
                }
                );
            }
        }
    }
}
