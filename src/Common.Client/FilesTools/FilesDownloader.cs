using System.Net.Http.Headers;
using System.Security.Cryptography;
using Common.Axiom;
using Common.Client.FilesTools.Interfaces;
using Microsoft.Extensions.Logging;

namespace Common.Client.FilesTools;

public sealed class FilesDownloader : IFilesDownloader
{
    private readonly ProgressReport _progressReport;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public FilesDownloader(
        ProgressReport progressReport,
        IHttpClientFactory httpClientFactory,
        ILogger logger
        )
    {
        _progressReport = progressReport;
        _httpClientFactory = httpClientFactory;
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


        using var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error while downloading {url}, error: {response.StatusCode}");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var contentLength = response.Content.Headers.ContentLength;

        _logger.LogInformation($"File length is {contentLength}");

        FileStream fileStream = new(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

        try
        {
            if (!contentLength.HasValue)
            {
                await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var buffer = new byte[81920];
                var totalBytesRead = 0f;
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    var res = totalBytesRead / (long)contentLength * 100;

                    _progressReport.OperationMessage = $"Downloading...";
                    ((IProgress<float>)_progressReport.Progress).Report(res);
                }
            }

            fileStream.Dispose();
            _logger.LogInformation("Downloading finished, renaming temp file");
            File.Move(tempFile, filePath, true);
        }
        catch (HttpIOException)
        {
            await ContinueDownload(url, contentLength, fileStream!, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            return new(ResultEnum.Cancelled, "Downloading cancelled");
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, ex.ToString());
        }
        finally
        {
            ((IProgress<float>)_progressReport.Progress).Report(0);
            _progressReport.OperationMessage = string.Empty;
            fileStream.Dispose();
        }

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
        _logger.LogInformation("Trying to continue downloading after failing");

        try
        {
            using HttpRequestMessage request = new()
            {
                RequestUri = url,
                Method = HttpMethod.Get
            };

            request.Headers.Range = new RangeHeaderValue(fileStream.Position, contentLength);

            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is not System.Net.HttpStatusCode.PartialContent)
            {
                throw new InvalidOperationException("Error while downloading a file: " + response.StatusCode);
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            await source.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpIOException)
        {
            await ContinueDownload(url, contentLength, fileStream, cancellationToken).ConfigureAwait(false);
        }
    }
}