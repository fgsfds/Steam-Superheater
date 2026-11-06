using System.Net;
using System.Net.Http.Headers;
using Common.Axiom;
using Common.Client.FilesTools.Interfaces;
using Microsoft.Extensions.Logging;

namespace Common.Client.FilesTools;

/// <summary>
/// Downloads to a temporary file and validates resumed responses before appending.
/// </summary>
public sealed class FilesDownloader(ProgressReport progressReport, IHttpClientFactory httpClientFactory, ILogger logger) : IFilesDownloader
{
    private const int MaxContinueDownloadAttempts = 5;

    /// <inheritdoc />
    public async Task<Result> DownloadFileAsync(Uri url, string filePath, CancellationToken cancellationToken)
    {
        var temporary = filePath + ".temp";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progressReport.OperationMessage = "Downloading...";
            using var client = httpClientFactory.CreateClient();
            await using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                long? expectedLength = null;
                EntityTagHeaderValue? etag = null;
                DateTimeOffset? modified = null;
                for (var attempt = 0; ; attempt++)
                {
                    var offset = file.Position;
                    var resume = attempt > 0 && expectedLength.HasValue && (etag is { IsWeak: false } || modified.HasValue);
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (resume)
                    {
                        request.Headers.Range = new RangeHeaderValue(offset, null);
                        request.Headers.IfRange = etag is { IsWeak: false }
                            ? new RangeConditionHeaderValue(etag)
                            : new RangeConditionHeaderValue(modified!.Value);
                    }

                    try
                    {
                        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                        if (response.StatusCode == HttpStatusCode.PartialContent)
                        {
                            var range = response.Content.Headers.ContentRange;
                            if (!resume || range?.Unit != "bytes" || range.From != offset || range.Length != expectedLength ||
                                range.To != expectedLength - 1 ||
                                (etag is { IsWeak: false } && !etag.Equals(response.Headers.ETag)) ||
                                (etag is not { IsWeak: false } && response.Content.Headers.LastModified != modified))
                            {
                                return new(ResultEnum.Error, "The resumed download does not match the requested file and byte range.");
                            }
                        }
                        else if (response.StatusCode == HttpStatusCode.OK)
                        {
                            file.SetLength(0);
                            file.Position = 0;
                            expectedLength = response.Content.Headers.ContentLength;
                            etag = response.Headers.ETag;
                            modified = response.Content.Headers.LastModified;
                        }
                        else
                        {
                            return new(ResultEnum.ConnectionError, $"Download failed: {response.StatusCode}");
                        }

                        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                            if (expectedLength is > 0)
                            {
                                ((IProgress<float>)progressReport.Progress).Report((float)((double)file.Position / expectedLength.Value * 100));
                            }
                        }

                        if (expectedLength.HasValue && file.Length != expectedLength)
                        {
                            throw new HttpIOException(HttpRequestError.ResponseEnded, "The downloaded file has an unexpected length.");
                        }

                        await file.FlushAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    catch (HttpIOException) when (attempt < MaxContinueDownloadAttempts)
                    {
                        logger.LogInformation("Retrying interrupted download, attempt {Attempt}", attempt + 1);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, filePath, true);
            return new(ResultEnum.Success, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return new(ResultEnum.Cancelled, "Downloading cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download failed");
            return new(ResultEnum.Error, ex.Message);
        }
        finally
        {
            ((IProgress<float>)progressReport.Progress).Report(0);
            progressReport.OperationMessage = string.Empty;
            try
            {
                if (File.Exists(temporary)) { File.Delete(temporary); }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Unable to remove partial download {Path}", temporary);
            }
        }
    }
}
