using Api.Axiom.Interfaces;
using Common.Axiom.Entities;

namespace Common.Client.Providers;

/// <summary>
/// Provides the S3 location settings loaded from data.json.
/// </summary>
public sealed class S3Provider
{
    private readonly IApiInterface _apiInterface;
    private readonly SemaphoreSlim _semaphore = new(1);

    private string? _bucket;
    private string? _endpoint;
    private string? _subFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3Provider" /> class.
    /// </summary>
    /// <param name="apiInterface">The API interface used to load data.json.</param>
    public S3Provider(IApiInterface apiInterface)
    {
        _apiInterface = apiInterface;
    }


    /// <summary>
    /// Builds the public URL of a file stored in the S3 bucket.
    /// </summary>
    /// <param name="relativePath">Path of the file relative to the S3 subfolder.</param>
    /// <returns>The public file URL.</returns>
    public async Task<string> GetFileUrlAsync(string relativePath)
    {
        await EnsureSettingsAsync().ConfigureAwait(false);

        return $"{_endpoint}/{_bucket}/{_subFolder}/{relativePath}";
    }

    /// <summary>
    /// Checks whether the URL points to the configured S3 endpoint.
    /// </summary>
    /// <param name="url">URL to check.</param>
    /// <returns>True when the URL belongs to the configured S3 endpoint.</returns>
    public async Task<bool> IsS3UrlAsync(string url)
    {
        await EnsureSettingsAsync().ConfigureAwait(false);

        return url.StartsWith(_endpoint!, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Loads and caches the S3 settings from data.json.
    /// </summary>
    private async Task EnsureSettingsAsync()
    {
        if (_endpoint is not null)
        {
            return;
        }

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_endpoint is not null)
            {
                return;
            }

            var dataResult = await _apiInterface.GetDataJsonAsync().ConfigureAwait(false);

            if (!dataResult.IsSuccess || dataResult.ResultObject is null)
            {
                throw new InvalidOperationException("Failed to load data.json.");
            }

            var data = dataResult.ResultObject;

            if (!data.TryGetValue(DataJson.S3Endpoint, out var endpoint) || string.IsNullOrWhiteSpace(endpoint) ||
                !data.TryGetValue(DataJson.S3Bucket, out var bucket) || string.IsNullOrWhiteSpace(bucket) ||
                !data.TryGetValue(DataJson.S3SubFolder, out var subFolder) || string.IsNullOrWhiteSpace(subFolder))
            {
                throw new InvalidOperationException("S3 settings are missing in data.json.");
            }

            _endpoint = endpoint.TrimEnd('/');
            _bucket = bucket;
            _subFolder = subFolder.Trim('/');
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }
}
