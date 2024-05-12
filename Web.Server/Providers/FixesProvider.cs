using Common.Entities.Fixes;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;
using Web.Server.Database;

namespace Superheater.Web.Server.Providers
{
    public sealed class FixesProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FixesProvider> _logger;
        private readonly string _jsonUrl = $"{Consts.FilesBucketUrl}fixes.json";

        private DateTime? _fixesListLastModified;
        private ImmutableList<FixesList> _fixesList;


        public ImmutableList<FixesList> FixesList => _fixesList;

        public int GamesCount { get; private set; }

        public int FixesCount { get; private set; }


        public FixesProvider(ILogger<FixesProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }


        public async Task CreateFixesListAsync()
        {
            _logger.LogInformation("Looking for new fixes");

            using var response = await _httpClient.GetAsync(_jsonUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error while getting response");
                return;
            }

            if (response.Content.Headers.LastModified is null)
            {
                _logger.LogError("Can't get fixes last modified date");
            }

            if (response.Content.Headers.LastModified <= _fixesListLastModified)
            {
                _logger.LogInformation("No new fixes found");
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var fixesList = JsonSerializer.Deserialize(json, FixesListContext.Default.ListFixesList);

            using var dbContext = new DatabaseContext();
            var installs = dbContext.Downloads.ToDictionary(x => x.FixGuid, x => x.Installs);
            var scores = dbContext.Rating.ToDictionary(x => x.FixGuid, x => x.Rating);

            foreach (var fixes in fixesList)
            {
                foreach (var fix in fixes.Fixes)
                {
                    var hasScore = scores.TryGetValue(fix.Guid, out var score);
                    fix.Score = hasScore ? score : 0;

                    var hasInstalls = installs.TryGetValue(fix.Guid, out var install);
                    fix.Installs = hasInstalls ? install : 0;
                }
            }

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
