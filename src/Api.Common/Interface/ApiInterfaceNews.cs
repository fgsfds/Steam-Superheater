using Api.Common.Messages;
using Common;
using Common.Entities;
using System.Net.Http.Json;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    public async Task<Result<List<NewsEntity>?>> GetNewsListAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl}/news").ConfigureAwait(false);

            if (response is null || !response.IsSuccessStatusCode)
            {
                return new(ResultEnum.Error, null, "Error while getting news");
            }

            var news = await response.Content.ReadFromJsonAsync(NewsEntityContext.Default.ListNewsEntity).ConfigureAwait(false);

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
            AddChangeNewsInMessage message = new()
            {
                Date = DateTime.Now,
                Content = content
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Post, $"{ApiUrl}/news/add");
            requestMessage.Headers.Authorization = new("admin", _configProvider.ApiPassword);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

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
            AddChangeNewsInMessage message = new()
            {
                Date = date,
                Content = content
            };

            using HttpRequestMessage requestMessage = new(HttpMethod.Put, $"{ApiUrl}/news/change");
            requestMessage.Headers.Authorization = new("admin", _configProvider.ApiPassword);

            var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

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
}
