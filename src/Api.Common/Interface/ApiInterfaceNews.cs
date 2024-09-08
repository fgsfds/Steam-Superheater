using Common;
using Common.Entities;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Common.Interface;

public sealed partial class ApiInterface
{
    public async Task<Result<List<NewsEntity>?>> GetNewsListAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_apiUrl}/news").ConfigureAwait(false);

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
            Tuple<DateTime, string, string> message = new(DateTime.Now, content, "");

            var response = await _httpClient.PostAsJsonAsync($"{_apiUrl}/news/add", message).ConfigureAwait(false);

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
            Tuple<DateTime, string, string> message = new(date, content, "");

            var response = await _httpClient.PutAsJsonAsync($"{_apiUrl}/news/change", message).ConfigureAwait(false);

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
