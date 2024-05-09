using Common;
using Common.Entities.Fixes;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class FixesProvider
    {
        private readonly HttpClientInstance _httpClient;
        private readonly ILogger<FixesProvider> _logger;
        private readonly string _jsonUrl = $"{Consts.FilesBucketUrl}fixes.json";

        private DateTime? _fixesListLastModified;
        private ImmutableList<FixesList> _fixesList;


        public ImmutableList<FixesList> FixesList => _fixesList;

        public int GamesCount { get; private set; }

        public int FixesCount { get; private set; }


        public FixesProvider(ILogger<FixesProvider> logger, HttpClientInstance httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }


        public async Task CreateFixesListAsync()
        {
            _logger.LogInformation("Looking for new fixes");

            using var response = await _httpClient.GetAsync(new(_jsonUrl), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (response.Content.Headers.LastModified is null)
            {
                _logger.LogError("Can't get fixes last modified date");

                if (_fixesListLastModified is not null &&
                    response.Content.Headers.LastModified <= _fixesListLastModified)
                {
                    _logger.LogInformation("No new fixes found");
                    return;
                }
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var fixesList = JsonSerializer.Deserialize(json, FixesListContext.Default.ListFixesList);

            Interlocked.Exchange(ref _fixesList, [.. fixesList]);

            FillCounts();

            if (response.Content.Headers.LastModified is not null)
            {
                _fixesListLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
            }

            _logger.LogInformation("Found new fixes");
        }

        private void FillCounts()
        {
            GamesCount = _fixesList.Count;

            var count = 0;

            foreach (var fix in _fixesList)
            {
                count += fix.Fixes.Count;
            }

            FixesCount = count;
        }
    }
}
