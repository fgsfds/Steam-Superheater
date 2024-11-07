using Common.Client.FilesTools.Interfaces;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Common.Client.FilesTools;

public sealed class FilesDownloader : IFilesDownloader
{
    private readonly ProgressReport _progressReport;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;


    public FilesDownloader(
        ProgressReport progressReport,
        HttpClient httpClient,
        ILogger logger
        )
    {
        _progressReport = progressReport;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> CheckAndDownloadFileAsync(
        Uri url,
        string filePath,
        CancellationToken cancellationToken
        )
    {
        _logger.LogInformation($"Started downloading file {url}");

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
            return new(ResultEnum.ConnectionError, "Error while downloading a file: " + response.StatusCode);
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

            return new(ResultEnum.Cancelled, "Downloading cancelled");
        }
        catch (HttpIOException)
        {
            await ContinueDownload(url, contentLength, fileStream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _progressReport.OperationMessage = string.Empty;
            return new(ResultEnum.Error, ex.ToString());
        }
        finally
        {
            await fileStream.DisposeAsync().ConfigureAwait(false);
        }

        File.Move(tempFile, filePath);

        _progressReport.OperationMessage = string.Empty;
        return new(ResultEnum.Success, string.Empty);
    }

    public async Task<Result> CheckFileHashAsync(string filePath, string hash, CancellationToken cancellationToken)
    {
        FileInfo fileInfo = new(filePath);
        var contentLength = fileInfo.Length;

        if (contentLength > 1e+9)
        {
            _progressReport.OperationMessage = "Checking hash... This may take a while. Press Cancel to skip.";
        }
        else
        {
            _progressReport.OperationMessage = "Checking hash...";
        }

        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);

            var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false));

            if (!hash.Equals(fileHash))
            {
                return new(ResultEnum.MD5Error, "File's hash doesn't match the database");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hash checking cancelled");
            //do nothing
        }
        catch (Exception ex)
        {
            _progressReport.OperationMessage = string.Empty;
            return new(ResultEnum.Error, ex.ToString());
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
    private async Task ContinueDownload(
        Uri url,
        long? contentLength,
        FileStream fileStream,
        CancellationToken cancellationToken
        )
    {
        try
        {
            using HttpRequestMessage request = new()
            {
                RequestUri = url,
                Method = HttpMethod.Get
            };

            request.Headers.Range = new RangeHeaderValue(fileStream.Position, contentLength);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is not System.Net.HttpStatusCode.PartialContent)
            {
                ThrowHelper.ThrowInvalidOperationException("Error while downloading a file: " + response.StatusCode);
            }

            using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpIOException)
        {
            await ContinueDownload(url, contentLength, fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Report operation progress
    /// </summary>
    /// <param name="streamToTrack">Stream</param>
    /// <param name="progress">Progress</param>
    /// <param name="contentLength">File size</param>
    private void TrackProgress(
        FileStream streamToTrack,
        IProgress<float> progress,
        long? contentLength
        )
    {
        if (contentLength is null)
        {
            return;
        }

        long prevPosition = default;
        var sw = Stopwatch.StartNew();

        while (streamToTrack.CanSeek)
        {
            var pos = streamToTrack.Position / (float)contentLength * 100;
            progress.Report(pos);

            if (sw.ElapsedMilliseconds >= 2000)
            {
                var bytes = streamToTrack.Position - prevPosition;
                var ms = sw.ElapsedMilliseconds;

                var mBytesS = (float)(bytes / ms) / 1024;

                _progressReport.OperationMessage = $"Downloading... ({mBytesS:0.0#}Mb/s)";

                prevPosition = streamToTrack.Position;
                sw.Restart();
            }

            Thread.Sleep(50);
        }
    }
}