using Web.Blazor.Providers;

namespace Web.Blazor.Tasks;

public sealed class StatsTask : BackgroundService
{
    private readonly StatsProvider _statsProvider;

    public StatsTask(StatsProvider statsProvider)
    {
        _statsProvider = statsProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _statsProvider.UpdateStats();

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
