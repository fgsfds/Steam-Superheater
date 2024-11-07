using Api.Common.Messages;
using Common;
using Common.Entities;
using Common.Enums;
using System.Net.Http.Json;
using System.Web;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    private readonly HttpClient _httpClient;
    private readonly IConfigProvider _configProvider;

    private string ApiUrl => _configProvider.UseLocalApiAndRepo ? "https://localhost:7093/api2" : "https://superheater.fgsfds.link/api2";

    public ApiInterface(
        HttpClient httpClient,
        IConfigProvider configProvider
        )
    {
        _configProvider = configProvider;
        _httpClient = httpClient;
    }

    public async Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        try
        {
            var encodedPath = HttpUtility.UrlEncode("superheater_uploads/" + path);

            using var response = await _httpClient.GetAsync($"{ApiUrl}/storage/url/{encodedPath}").ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting signed URL");
            }

            var url = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return new(ResultEnum.Success, url, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch (Exception)
        {
            return new(ResultEnum.Error, null, "Error while getting signed URL");
        }
    }

    public async Task<Result<AppReleaseEntity?>> GetLatestAppReleaseAsync(OSEnum osEnum)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{ApiUrl}/releases").ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting latest release");
            }

            var releases = await response.Content.ReadFromJsonAsync(GetReleasesOutMessageContext.Default.GetReleasesOutMessage).ConfigureAwait(false);

            return new(ResultEnum.Success, releases!.Releases[osEnum], string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while getting latest release");
        }
    }
}
