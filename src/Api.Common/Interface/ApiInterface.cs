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
    private readonly string _apiUrl;

    public ApiInterface(
        HttpClient httpClient
        )
    {
        _apiUrl = "https://superheater.fgsfds.link/api2";
        _httpClient = httpClient;
    }

    public async Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        try
        {
            var encodedPath = HttpUtility.UrlEncode("superheater_uploads/" + path);

            var response = await _httpClient.GetAsync($"{_apiUrl}/storage/url/{encodedPath}").ConfigureAwait(false);

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
            var response = await _httpClient.GetAsync($"{_apiUrl}/releases").ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting latest release");
            }

            var releases = await response.Content.ReadFromJsonAsync<GetReleasesOutMessage>().ConfigureAwait(false);

            return new(ResultEnum.Success, releases.Releases[osEnum], string.Empty);
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
