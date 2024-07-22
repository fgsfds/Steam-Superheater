using Common.Helpers;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Common.Client.FilesTools
{
    public sealed class FilesDownloader
    {
        private readonly ProgressReport _progressReport;
        private readonly HttpClient _httpClient;
        private readonly Logger _logger;


        public FilesDownloader(
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

            //Hash check before download
            if (hash is not null)
            {
                if (url.ToString().StartsWith(Consts.FilesBucketUrl))
                {
                    if (response.Headers.ETag?.Tag is not null)
                    {
                        var md5 = response.Headers.ETag!.Tag.Replace("\"", "");

                        if (!md5.Contains('-') &&
                            !md5.Equals(hash, StringComparison.OrdinalIgnoreCase))
                        {
                            return new(ResultEnum.MD5Error, "File's hash doesn't match the database");
                        }
                    }
                }
                else if (response.Content.Headers.ContentMD5 is not null &&
                         !hash.Equals(Convert.ToHexString(response.Content.Headers.ContentMD5)))
                {
                    return new(ResultEnum.MD5Error, "File's hash doesn't match the database");
                }
            }

            //Downloading
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
            catch (HttpIOException)
            {
                await ContinueDownload(url, contentLength, fileStream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new(ResultEnum.Error, ex.ToString());
            }
            finally
            {
                await fileStream.DisposeAsync().ConfigureAwait(false);
            }

            File.Move(tempFile, filePath);

            //Hash check of downloaded file
            if (hash is not null)
            {
                _progressReport.OperationMessage = "Checking hash...";

                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);

                var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream).ConfigureAwait(false));

                if (!hash.Equals(fileHash))
                {
                    return new(ResultEnum.MD5Error, "Downloaded file's hash doesn't match the database");
                }
            }

            _progressReport.OperationMessage = string.Empty;
            return new(ResultEnum.Success, string.Empty);
        }


        /// <summary>
        /// Continue download after network error
        /// </summary>
        /// <param name="url">Url to the file</param>
        /// <param name="contentLength">Total content length</param>
        /// <param name="fileStream">File stream to write to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ContinueDownload(Uri url, long? contentLength, FileStream fileStream, CancellationToken cancellationToken)
        {
            try
            {
                using HttpRequestMessage request = new()
                {
                    RequestUri = url,
                    Method = HttpMethod.Get
                };

                request.Headers.Range = new RangeHeaderValue(fileStream.Position, contentLength);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (response.StatusCode is not System.Net.HttpStatusCode.PartialContent)
                {
                    ThrowHelper.Exception("Error while downloading file");
                }

                using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpIOException)
            {
                await ContinueDownload(url, contentLength, fileStream, cancellationToken).ConfigureAwait(false);
            }
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
