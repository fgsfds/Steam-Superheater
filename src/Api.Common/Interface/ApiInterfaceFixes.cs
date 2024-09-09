using Api.Common.Messages;
using Common;
using Common.Entities.Fixes;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    public async Task<Result<List<FixesList>?>> GetFixesListAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl}/fixes").ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting fixes");
            }

            var fixesList = await response.Content.ReadFromJsonAsync(FixesListContext.Default.ListFixesList).ConfigureAwait(false);

            if (fixesList is null)
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
            ChangeScoreInMessage message = new()
            {
                FixGuid = guid,
                Increment = increment
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Put, $"{ApiUrl}/fixes/score/change");
            requestMessage.Headers.Authorization = new(AuthenticationSchemes.Basic.ToString(), _configProvider.ApiPassword);
            requestMessage.Content = JsonContent.Create(message, ChangeScoreInMessageContext.Default.ChangeScoreInMessage);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while chaging fix score");
            }

            var result = await response.Content.ReadFromJsonAsync(ChangeScoreOutMessageContext.Default.ChangeScoreOutMessage).ConfigureAwait(false);

            if (result is null)
            {
                return new(ResultEnum.Error, null, "Error while deserializing new score");
            }

            return new(ResultEnum.Success, result.Score, string.Empty);
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
            IncreaseInstallsCountInMessage message = new()
            {
                FixGuid = guid
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Put, $"{ApiUrl}/fixes/score/add");
            requestMessage.Headers.Authorization = new(AuthenticationSchemes.Basic.ToString(), _configProvider.ApiPassword);
            requestMessage.Content = JsonContent.Create(message, IncreaseInstallsCountInMessageContext.Default.IncreaseInstallsCountInMessage);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while chaging fix score");
            }

            var result = await response.Content.ReadFromJsonAsync(IncreaseInstallsCountOutMessageContext.Default.IncreaseInstallsCountOutMessage).ConfigureAwait(false);

            if (result is null)
            {
                return new(ResultEnum.Error, null, "Error while deserializing new score");
            }

            return new(ResultEnum.Success, result.InstallsCount, string.Empty);
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
            ReportFixInMessage message = new()
            {
                FixGuid = guid,
                Text = text
            };

            using var response = await _httpClient.PostAsJsonAsync($"{ApiUrl}/fixes/report", message, ReportFixInMessageContext.Default.ReportFixInMessage).ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
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

    public async Task<Result<int?>> CheckIfFixExistsAsync(Guid guid)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl}/fixes/exists?guid={guid}").ConfigureAwait(false);

            if (response is null)
            {
                return new(ResultEnum.Error, null, "Error while checking fix");
            }

            var result = await response.Content.ReadFromJsonAsync(CheckIfFixExistsOutMessageContext.Default.CheckIfFixExistsOutMessage).ConfigureAwait(false);

            if (result is null)
            {
                return new(ResultEnum.Error, null, "Error while deserializing result");
            }

            return new(ResultEnum.Success, result.CurrentVersion, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while checking fix");
        }
    }

    public async Task<Result> ChangeFixStateAsync(Guid guid, bool isDeleted)
    {
        try
        {
            Tuple<Guid, bool, string> message = new(guid, isDeleted, "");

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

            Tuple<int, string, string, string> message = new(gameId, gameName, jsonStr, "");

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
