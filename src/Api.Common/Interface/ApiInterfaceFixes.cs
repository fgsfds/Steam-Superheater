using Common;
using Common.Entities.Fixes;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    public async Task<Result<List<FixesList>?>> GetFixesListAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_apiUrl}/fixes").ConfigureAwait(false);

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
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/fixes/score/change", new Tuple<Guid, sbyte>(guid, increment)).ConfigureAwait(false);

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
            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/fixes/installs/add", guid).ConfigureAwait(false);

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
            using var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/fixes/report", new Tuple<Guid, string>(guid, text)).ConfigureAwait(false);

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
            var result = await _httpClient.GetStringAsync($"{_apiUrl}/fixes/{guid}").ConfigureAwait(false);

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
            Tuple<Guid, bool, string> message = new(guid, isDeleted, "");

            var result = await _httpClient.PutAsJsonAsync($"{_apiUrl}/fixes/delete", message).ConfigureAwait(false);

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

            Tuple<int, string, string, string> message = new(gameId, gameName, jsonStr, "");

            var result = await _httpClient.PostAsJsonAsync($"{_apiUrl}/fixes/add", message).ConfigureAwait(false);

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
