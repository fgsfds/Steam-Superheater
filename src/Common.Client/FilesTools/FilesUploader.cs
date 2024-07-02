using Common.Client.API;

namespace Common.Client.FilesTools
{
    public sealed class FilesUploader
    {
        private readonly Logger _logger;
        private readonly ApiInterface _apiInterface;
        private readonly ProgressReport _progressReport;


        public FilesUploader(
            Logger logger,
            ApiInterface apiInterface,
            ProgressReport progressReport)
        {
            _logger = logger;
            _apiInterface = apiInterface;
            _progressReport = progressReport;
        }


        /// <summary>
        /// Upload single file to S3
        /// </summary>
        /// <param name="folder">Destination folder in the bucket</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <param name="remoteFileName">File name on the s3 server</param>
        /// <returns>True if successfully uploaded</returns>
        public async Task<Result> UploadFilesToFtpAsync(string folder, string filePath, string remoteFileName, CancellationToken cancellationToken)
        {
            return await UploadFilesToFtpAsync(folder, [filePath], cancellationToken, remoteFileName).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload multiple files to S3
        /// </summary>
        /// <param name="folder">Destination folder in the bucket</param>
        /// <param name="files">List of paths to files</param>
        /// <param name="remoteFileName">File name on the s3 server</param>
        /// <returns>True if successfully uploaded</returns>
        public async Task<Result> UploadFilesToFtpAsync(string folder, List<string> files, CancellationToken cancellationToken, string? remoteFileName = null)
        {
            _logger.Info($"Uploading {files.Count} file(s)");

            _progressReport.OperationMessage = "Uploading...";
            IProgress<float> progress = _progressReport.Progress;

            try
            {
                foreach (var file in files)
                {
                    var fileName = remoteFileName ?? Path.GetFileName(file);
                    var result = await _apiInterface.GetSignedUrlAsync(folder + "/" + fileName).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        return new(result.ResultEnum, result.Message);
                    }

                    using var fileStream = File.OpenRead(file);
                    using StreamContent content = new(fileStream);

                    new Task(() => { TrackProgress(fileStream, progress); }).Start();

                    using HttpClient httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

                    using var response = await httpClient.PutAsync(result.ResultObject, content, cancellationToken).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return new(ResultEnum.Error, errorMessage);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Uploading cancelled");
                return new(ResultEnum.Error, "Uploading cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                return new(ResultEnum.Error, ex.Message);
            }
            finally
            {
                _progressReport.OperationMessage = string.Empty;
            }

            return new(ResultEnum.Success, string.Empty);
        }


        private static void TrackProgress(FileStream streamToTrack, IProgress<float> progress)
        {
            while (streamToTrack.CanSeek)
            {
                var pos = streamToTrack.Position / (float)streamToTrack.Length * 100;
                progress.Report(pos);

                Thread.Sleep(50);
            }
        }
    }
}
