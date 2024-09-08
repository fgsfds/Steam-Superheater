using Common;
using Common.Entities;
using Common.Enums;
using System.Text.Json;
using System.Web;

namespace Api.Common;

public sealed partial class ApiInterface
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public ApiInterface(
        HttpClient httpClient
        )
    {
        _apiUrl = "https://superheater.fgsfds.link/api2";
        _apiUrl = "http://localhost:7126/api2";
        _httpClient = httpClient;
    }

    public async Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        try
        {
            var encodedPath = HttpUtility.UrlEncode("superheater_uploads/" + path);

            var signedUrl = await _httpClient.GetStringAsync($"{_apiUrl}/storage/url/{encodedPath}").ConfigureAwait(false);

            if (signedUrl is null)
            {
                return new(ResultEnum.Error, null, "Error while getting signed URL");
            }

            return new(ResultEnum.Success, signedUrl, string.Empty);
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
            var response = await _httpClient.GetStringAsync($"{_apiUrl}/release/{osEnum.ToString().ToLower()}").ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                return new(ResultEnum.Error, null, "Error while getting latest release");
            }

            var release = JsonSerializer.Deserialize(response, AppReleaseEntityContext.Default.AppReleaseEntity);

            return new(ResultEnum.Success, release, string.Empty);
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
