using Common.Helpers;
using System.Web;

namespace Common
{
    public class FilesUploader
    {
        private readonly HttpClientInstance _httpClient;
        private readonly Logger _logger;
        private readonly ProgressReport _progressReport;

        public FilesUploader(
            HttpClientInstance httpClient,
            Logger logger,
            ProgressReport progressReport)
        {
            _httpClient = httpClient;
            _logger = logger;
            _progressReport = progressReport;
        }

        /// <summary>
        /// Upload single file to S3
        /// </summary>
        /// <param name="folder">Destination folder in the bucket</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <param name="remoteFileName">File name on the s3 server</param>
        /// <returns>True if successfully uploaded</returns>
        public async Task<Result> UploadFilesToFtpAsync(string folder, string filePath, string remoteFileName)
        {
            return await UploadFilesToFtpAsync(folder, [filePath], remoteFileName).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload multiple files to S3
        /// </summary>
        /// <param name="folder">Destination folder in the bucket</param>
        /// <param name="files">List of paths to files</param>
        /// <param name="remoteFileName">File name on the s3 server</param>
        /// <returns>True if successfully uploaded</returns>
        public async Task<Result> UploadFilesToFtpAsync(string folder, List<string> files, string? remoteFileName = null)
        {
            _logger.Info($"Uploading {files.Count} file(s)");

            _progressReport.OperationMessage = "Uploading...";
            IProgress<float> progress = _progressReport.Progress;

            try
            {
                foreach (var file in files)
                {
                    var fileName = remoteFileName ?? Path.GetFileName(file);
                    var path = "superheater_uploads/" + folder + "/" + fileName;
                    var encodedPath = HttpUtility.UrlEncode(path);

                    var signedUrl = await _httpClient.GetStringAsync($"{CommonProperties.ApiUrl}/storage/url/{encodedPath}").ConfigureAwait(false);

                    using var stream = File.OpenRead(file);
                    using StreamContent content = new(stream);

                    var keepTracking = true;
                    new Task(new Action(() => { TrackProgress(stream, progress, ref keepTracking); })).Start();
                    await _httpClient.PutAsync(signedUrl, content).ConfigureAwait(false);
                    keepTracking = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }
            finally
            {
                _progressReport.OperationMessage = string.Empty;
            }

            return new(ResultEnum.Success, string.Empty);
        }

        private void TrackProgress(FileStream streamToTrack, IProgress<float> progress, ref bool keepTracking)
        {
            while (keepTracking)
            {
                var pos = ((float)streamToTrack.Position / (float)streamToTrack.Length) * 100;
                progress.Report(pos);

                Thread.Sleep(100);
            }
        }
    }
}
