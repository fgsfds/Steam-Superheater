using System.Security.Cryptography;
using Common.Axiom;
using Common.Client.FilesTools.Interfaces;
using Downloader;
using Microsoft.Extensions.Logging;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace Common.Client.FilesTools;

public sealed class FilesDownloader : IFilesDownloader
{
    private readonly ProgressReport _progressReport;
    private readonly DownloadService _downloadService;
    private readonly ILogger _logger;


    public FilesDownloader(
        ProgressReport progressReport,
        DownloadService downloadService,
        ILogger logger
        )
    {
        _progressReport = progressReport;
        _downloadService = downloadService;
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



        try
        {
            _downloadService.DownloadProgressChanged += OnDownloadProgressChangedEvent;
            await _downloadService.DownloadFileTaskAsync(url.ToString(), tempFile, cancellationToken).ConfigureAwait(false);

            if (_downloadService.Status is DownloadStatus.Stopped or DownloadStatus.Failed)
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                return new(ResultEnum.Cancelled, "Downloading cancelled");
            }
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, ex.ToString());
        }
        finally
        {
            ((IProgress<float>)_progressReport.Progress).Report(0);
            _progressReport.OperationMessage = string.Empty;
            _downloadService.DownloadProgressChanged -= OnDownloadProgressChangedEvent;
        }

        File.Move(tempFile, filePath);

        return new(ResultEnum.Success, string.Empty);
    }

    private void OnDownloadProgressChangedEvent(object? sender, DownloadProgressChangedEventArgs e)
    {
        _progressReport.OperationMessage = $"Downloading... ({e.BytesPerSecondSpeed / 1000000:0.0#}Mb/s, {e.AverageBytesPerSecondSpeed / 1000000:0.0#}Mb/s average)";
        ((IProgress<float>)_progressReport.Progress).Report((float)e.ProgressPercentage);
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
            await using var stream = File.OpenRead(filePath);

            var fileHash = Convert.ToHexString(await md5.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false));

            if (!hash.Equals(fileHash, StringComparison.OrdinalIgnoreCase))
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
}