using Common.Client.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace Common.Client.API;

public sealed class ApiInterface
{
    private readonly HttpClient _httpClient;
    private readonly IConfigProvider _config;

    private string ApiUrl => _config.UseLocalApiAndRepo ? "https://localhost:7093/api" : "https://superheater.fgsfds.link/api";

    public ApiInterface(
        IConfigProvider configProvider,
        HttpClient httpClient
        )
    {
        _config = configProvider;
        _httpClient = httpClient;
    }

    public async Task<Result<List<NewsEntity>?>> GetNewsListAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{ApiUrl}/news").ConfigureAwait(false);

            if (response is null)
            {
                return new(ResultEnum.Error, null, "Error while getting news");
            }

            var news = JsonSerializer.Deserialize(response, NewsEntityContext.Default.ListNewsEntity);

            if (news is null)
            {
                return new(ResultEnum.Error, null, "Error while deserializing news");
            }

            return new(ResultEnum.Success, news, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.ConnectionError, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while getting news");
        }
    }

    public async Task<Result> AddNewsAsync(string content)
    {
        try
        {
            Tuple<DateTime, string, string> message = new(DateTime.Now, content, _config.ApiPassword);

            var response = await _httpClient.PostAsJsonAsync($"{ApiUrl}/news/add", message).ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while adding news");
            }

            return new(ResultEnum.Success, "Succesfully added news");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.ConnectionError, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while adding news");
        }
    }

    public async Task<Result> ChangeNewsAsync(DateTime date, string content)
    {
        try
        {
            Tuple<DateTime, string, string> message = new(date, content, _config.ApiPassword);

            var response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/news/change", message).ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while changing news");
            }

            return new(ResultEnum.Success, "Succesfully changed news");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.ConnectionError, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while changing news");
        }
    }

    public async Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        try
        {
            var encodedPath = HttpUtility.UrlEncode("superheater_uploads/" + path);

            var signedUrl = await _httpClient.GetStringAsync($"{ApiUrl}/storage/url/{encodedPath}").ConfigureAwait(false);

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
            var response = await _httpClient.GetStringAsync($"{ApiUrl}/release/{osEnum.ToString().ToLower()}").ConfigureAwait(false);

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

    public async Task<Result<List<FixesList>?>> GetFixesListAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{ApiUrl}/fixes").ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                return new(ResultEnum.Error, null, "Error while getting fixes");
            }

            var fixesList = JsonSerializer.Deserialize(response, FixesListContext.Default.ListFixesList);

            if (string.IsNullOrWhiteSpace(response))
            {
                return new(ResultEnum.Error, null, "Error while deserializing fixes");
            }

            return new(ResultEnum.Success, fixesList, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while getting fixes");
        }
    }

    public async Task<Result<int?>> ChangeScoreAsync(Guid guid, sbyte increment)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/fixes/score/change", new Tuple<Guid, sbyte>(guid, increment)).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while chaging fix score");
            }

            var responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var isParsed = int.TryParse(responseStr, out var newScore);

            if (!isParsed)
            {
                return new(ResultEnum.Error, null, "Error while deserializing new score");
            }

            return new(ResultEnum.Success, newScore, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while chaging fix score");
        }
    }

    public async Task<Result<int?>> AddNumberOfInstallsAsync(Guid guid)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/fixes/installs/add", guid).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while chaging fix score");
            }

            var responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var isParsed = int.TryParse(responseStr, out var newInatallsNum);

            if (!isParsed)
            {
                return new(ResultEnum.Error, null, "Error while deserializing new score");
            }

            return new(ResultEnum.Success, newInatallsNum, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while chaging fix score");
        }
    }

    public async Task<Result> ReportFixAsync(Guid guid, string text)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync($"{ApiUrl}/fixes/report", new Tuple<Guid, string>(guid, text)).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while sending report");
            }

            return new(ResultEnum.Success, "Succesfully sent report");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while sending report");
        }
    }

    public async Task<Result> CheckIfFixEsistsAsync(Guid guid)
    {
        try
        {
            var result = await _httpClient.GetStringAsync($"{ApiUrl}/fixes/{guid}").ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
            {
                return new(ResultEnum.Error, "Error while checking fix");
            }

            var isParsed = bool.TryParse(result, out var doesExist);

            if (!isParsed)
            {
                return new(ResultEnum.Error, "Error while deserializing result");
            }

            return new(doesExist ? ResultEnum.Exists : ResultEnum.NotFound, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while checking fix");
        }
    }

    public async Task<Result> ChangeFixStateAsync(Guid guid, bool isDeleted)
    {
        try
        {
            Tuple<Guid, bool, string> message = new(guid, isDeleted, _config.ApiPassword);

            var result = await _httpClient.PutAsJsonAsync($"{ApiUrl}/fixes/delete", message).ConfigureAwait(false);

            if (!result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while changing fix state");
            }

            return new(ResultEnum.Success, $"Succesfully {(isDeleted ? "disabled" : "enbaled")} fix");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while changing fix state");
        }
    }

    public async Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix)
    {
        try
        {
            var jsonStr = JsonSerializer.Serialize(
                fix,
                FixesListContext.Default.BaseFixEntity
                );

            Tuple<int, string, string, string> message = new(gameId, gameName, jsonStr, _config.ApiPassword);

            var result = await _httpClient.PostAsJsonAsync($"{ApiUrl}/fixes/add", message).ConfigureAwait(false);

            if (!result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while adding fix");
            }

            return new(ResultEnum.Success, "Succesfully added fix to the database");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, "Error while adding fix");
        }
    }
}

