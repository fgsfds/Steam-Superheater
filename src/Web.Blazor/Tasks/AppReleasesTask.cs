using Common.Axiom.Providers;

namespace Web.Blazor.Tasks;

public sealed class AppReleasesTask : BackgroundService
{
    private readonly AppReleasesProvider _appReleasesProvider;

    public AppReleasesTask(AppReleasesProvider appReleasesProvider)
    {
        _appReleasesProvider = appReleasesProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _ = _appReleasesProvider.GetLatestVersionAsync();

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
