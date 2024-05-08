using Common;
using Common.Entities.Fixes;
using Superheater.Web.Server.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class FixesProvider
    {
        private readonly HttpClientInstance _httpClient;
        private readonly ILogger<FixesProvider> _logger;
        private readonly string _jsonUrl = $"{Properties.FilesBucketUrl}fixes.json";

        private DateTime? _fixesListLastModified;
        private ImmutableList<FixesList> _fixesList;
        private int _fixesCount;
        private int _gamesCount;


        public ImmutableList<FixesList> FixesList => _fixesList;

        public int GamesCount => _gamesCount;

        public int FixesCount => _fixesCount;


        public FixesProvider(ILogger<FixesProvider> logger, HttpClientInstance httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }


        public async Task CreateFixesList()
        {
            using var response = await _httpClient.GetAsync(new(_jsonUrl), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (response.Content.Headers.LastModified is null)
            {
                _logger.LogError("Can't get last modified date");
                return;
            }

            if (_fixesListLastModified is not null &&
                response.Content.Headers.LastModified <= _fixesListLastModified)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var fixesList = JsonSerializer.Deserialize(json, FixesListContext.Default.ListFixesList);

            Interlocked.Exchange(ref _fixesList, [.. fixesList]);

            FillCounts();

            if (response.Content.Headers.LastModified is not null)
            {
                _fixesListLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
            }
        }

        private void FillCounts()
        {
            _gamesCount = _fixesList.Count;

            var count = 0;

            foreach (var fix in _fixesList)
            {
                count += fix.Fixes.Count;
            }

            _fixesCount = count;
        }
    }
}
