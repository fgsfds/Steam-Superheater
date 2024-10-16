using Web.Blazor.Providers;

namespace Web.Blazor.Tasks;

public sealed class FileCheckerTask : BackgroundService
{
    private readonly FixesProvider _fixesProvider;

    public FileCheckerTask(FixesProvider fixesProvider)
    {
        _fixesProvider = fixesProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _ = _fixesProvider.CheckFixesAsync();

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
