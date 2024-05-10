namespace Common
{
    public sealed class HttpClientInstance : IDisposable
    {
        private readonly HttpClient _httpClient;

        public HttpClientInstance()
        {
            _httpClient = new();
            _httpClient.Timeout = TimeSpan.FromMinutes(1);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Superheater");
        }

        /// <summary>
        /// Send a GET request to the specified Uri and return the response body as a string
        /// </summary>
        /// <param name="url">Uri</param>
        public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content)
        {
            var result = await _httpClient.PutAsync(url, content).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Send a GET request to the specified Uri and return the response body as a string
        /// </summary>
        /// <param name="url">Uri</param>
        public async Task<string> GetStringAsync(string url) => await GetStringAsync(new Uri(url)).ConfigureAwait(false);

        /// <summary>
        /// Send a GET request to the specified Uri and return the response body as a string
        /// </summary>
        /// <param name="url">Uri</param>
        public async Task<string> GetStringAsync(Uri url)
        {
            var result = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
            return result;
        }

        public async Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            return await GetAsync(new Uri(url), option).ConfigureAwait(false);
        }


        /// <summary>
        /// Send a GET request to the specified Uri and return the response body as a string
        /// </summary>
        /// <param name="url">Uri</param>
        public async Task<HttpResponseMessage> GetAsync(Uri url, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            var result = await _httpClient.GetAsync(url, option).ConfigureAwait(false);
            return result;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
