using Api.Common.Messages;
using Common;
using Common.Entities.Fixes;
using System.Net;
using System.Net.Http.Json;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    public async Task<Result<GetFixesOutMessage?>> GetFixesListAsync(int tableVersion, Version appVersion)
    {
        try
        {
            GetFixesInMessage message = new()
            {
                TableVersion = tableVersion,
                AppVersion = appVersion
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Get, $"{ApiUrl}/fixes");
            requestMessage.Content = JsonContent.Create(message, GetFixesInMessageContext.Default.GetFixesInMessage);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting fixes");
            }

            if (response.StatusCode is HttpStatusCode.NoContent)
            {
                return new(ResultEnum.Success, null, "No updated fixes found");
            }

            var fixesList = await response.Content.ReadFromJsonAsync(GetFixesOutMessageContext.Default.GetFixesOutMessage).ConfigureAwait(false);

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

            var response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/fixes/score", message, ChangeScoreInMessageContext.Default.ChangeScoreInMessage).ConfigureAwait(false);

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

            var response = await _httpClient.PutAsJsonAsync($"{ApiUrl}/fixes/installs", message, IncreaseInstallsCountInMessageContext.Default.IncreaseInstallsCountInMessage).ConfigureAwait(false);

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

    public async Task<Result> ChangeFixStateAsync(Guid guid, bool isDisabled)
    {
        try
        {
            ChangeFixStateInMessage message = new()
            {
                FixGuid = guid,
                IsDisabled = isDisabled
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Put, $"{ApiUrl}/fixes/state");
            requestMessage.Headers.Authorization = new(AuthenticationSchemes.Basic.ToString(), _configProvider.ApiPassword);
            requestMessage.Content = JsonContent.Create(message, ChangeFixStateInMessageContext.Default.ChangeFixStateInMessage);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, "Error while changing fix state");
            }

            return new(ResultEnum.Success, $"Succesfully {(isDisabled ? "disabled" : "enbaled")} fix");
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
            AddFixInMessage message = new()
            {
                GameId = gameId,
                GameName = gameName,
                Fix = fix
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, $"{ApiUrl}/fixes");
            requestMessage.Headers.Authorization = new(AuthenticationSchemes.Basic.ToString(), _configProvider.ApiPassword);
            requestMessage.Content = JsonContent.Create(message, AddFixInMessageContext.Default.AddFixInMessage);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
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

    public async Task<Result<GetFixesStatsOutMessage>> GetFixesStats()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl}/fixes/stats").ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting fixes stats");
            }

            var result = await response.Content.ReadFromJsonAsync(GetFixesStatsOutMessageContext.Default.GetFixesStatsOutMessage).ConfigureAwait(false);

            if (result is null)
            {
                return new(ResultEnum.Error, null, "Error while deserializing result");
            }

            return new(ResultEnum.Success, result, "Succesfully got fixes stats");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(ResultEnum.Error, null, "API is not responding");
        }
        catch
        {
            return new(ResultEnum.Error, null, "Error while getting fixes stats");
        }
    }
}
