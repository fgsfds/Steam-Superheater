using Common.Helpers;
using SharpCompress.Archives;
using System.Security.Cryptography;

namespace Common.Client
{
    /// <summary>
    /// Class for working with archives
    /// </summary>
    public sealed class ArchiveTools
    {
        private readonly ProgressReport _progressReport;
        private readonly HttpClient _httpClient;
        private readonly Logger _logger;


        public ArchiveTools(
            ProgressReport progressReport, 
            HttpClient httpClient,
            Logger logger
        )
        {
            _progressReport = progressReport;
            _httpClient = httpClient;
            _logger = logger;
        }


        /// <summary>
        /// Download ZIP
        /// </summary>
        /// <param name="url">Link to file download</param>
        /// <param name="filePath">Absolute path to destination file</param>
        /// <param name="cancellationToken"></param>
        /// <param name="hash">MD5 to check file against</param>
        /// <exception cref="Exception">Error while downloading file</exception>
        public async Task<Result> CheckAndDownloadFileAsync(
            Uri url,
            string filePath,
            CancellationToken cancellationToken,
            string? hash = null)
        {
            _logger.Info($"Started downloading file {url}");

            IProgress<float> progress = _progressReport.Progress;
            var tempFile = filePath + ".temp";

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            _progressReport.OperationMessage = "Downloading...";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                ThrowHelper.Exception("Error while downloading a file: " + response.StatusCode);
            }

            if (hash is not null)
            {
                if (url.ToString().StartsWith(Consts.FilesBucketUrl))
                {
                    if (response.Headers.ETag?.Tag is not null)
                    {
                        var md5 = response.Headers.ETag!.Tag.Replace("\"", "");

                        if (!md5.Contains('-') &&
                            !md5.Equals(hash, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return new(ResultEnum.MD5Error, string.Empty);
                        }
                    }
                }
                else if (response.Content.Headers.ContentMD5 is not null &&
                         !hash.Equals(Convert.ToHexString(response.Content.Headers.ContentMD5)))
                {
                    return new(ResultEnum.MD5Error, string.Empty);
                }
            }

            await using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var contentLength = response.Content.Headers.ContentLength;

            FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            new Task(() => { TrackProgress(fileStream, progress, contentLength); }).Start();
             
            try
            {
                await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                await fileStream.DisposeAsync().ConfigureAwait(false);

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                ThrowHelper.Exception("Downloading cancelled");
            }

            await fileStream.DisposeAsync().ConfigureAwait(false);
            File.Move(tempFile, filePath);

            if (hash is not null)
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);

                var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream).ConfigureAwait(false));

                if (!hash.Equals(fileHash))
                {
                    return new(ResultEnum.MD5Error, string.Empty);
                }
            }

            _progressReport.OperationMessage = string.Empty;
            return new(ResultEnum.Success, string.Empty);
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
                        using var writableStream = File.OpenWrite(fullName);
                        reader.WriteEntryTo(writableStream);
                    }

                    var value = entryNumber / entriesCount * 100;
                    progress.Report(value);

                    entryNumber++;
                }
            }).ConfigureAwait(false);

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

        private static void TrackProgress(FileStream streamToTrack, IProgress<float> progress, long? contentLength)
        {
            if (contentLength is null)
            {
                return;
            }

            while (streamToTrack.CanSeek)
            {
                var pos = streamToTrack.Position / (float)contentLength * 100;
                progress.Report(pos);

                Thread.Sleep(50);
            }
        }
    }
}
