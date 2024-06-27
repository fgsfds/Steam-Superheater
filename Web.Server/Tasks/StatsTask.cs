using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Tasks
{
    public sealed class StatsTask : IHostedService, IDisposable
    {
        private readonly ILogger<AppReleasesTask> _logger;
        private readonly StatsProvider _statsProvider;

        private Timer _timer;

        public StatsTask(
            ILogger<AppReleasesTask> logger,
            StatsProvider statsProvider
            )
        {
            _logger = logger;
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
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}