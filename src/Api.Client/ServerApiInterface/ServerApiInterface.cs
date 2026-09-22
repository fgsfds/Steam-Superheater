using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Api.Axiom.Interfaces;
using Api.Axiom.Messages;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Common.Client;

namespace Api.Client.ServerApiInterface;

public sealed partial class ServerApiInterface : IApiInterface
{
    private readonly HttpClient _httpClient;
    private readonly IConfigProvider _configProvider;

    private string ApiUrl => _configProvider.UseLocalApiAndRepo ? "https://localhost:7093/api2" : "https://superheater.fgsfds.link/api2";

    public ServerApiInterface(
        HttpClient httpClient,
        IConfigProvider configProvider
        )
    {
        _configProvider = configProvider;
        _httpClient = httpClient;
    }

    public async Task<Result<IReadOnlyDictionary<string, string>?>> GetDataJsonAsync()
    {
        var path = ClientProperties.PathToLocalDataJson;

        if (path is null)
        {
            return new(ResultEnum.NotFound, null, "data.json not found");
        }

        try
        {
            await using var stream = File.OpenRead(path);

            var data = await JsonSerializer.DeserializeAsync(stream, DataJsonModelContext.Default.DictionaryStringString).ConfigureAwait(false);

            return new(ResultEnum.Success, data, string.Empty);
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, null, ex.Message);
        }
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

            if (releases is null || !releases.Releases.TryGetValue(osEnum, out var release))
            {
                return new(ResultEnum.NotFound, null, "Release not found");
            }

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
