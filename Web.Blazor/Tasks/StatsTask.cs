using Web.Blazor.Providers;

namespace Web.Blazor.Tasks;

public sealed class StatsTask : IHostedService, IDisposable
{
    private readonly StatsProvider _statsProvider;

    private Timer _timer;

    public StatsTask(
        StatsProvider statsProvider
        )
    {
        _statsProvider = statsProvider;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.Zero,
            TimeSpan.FromHours(6)
            );

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _statsProvider.UpdateStats();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _ = _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

