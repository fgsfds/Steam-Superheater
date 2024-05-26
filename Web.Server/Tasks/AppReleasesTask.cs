using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Tasks
{
    public sealed class AppReleasesTask : IHostedService, IDisposable
    {
        private readonly ILogger<AppReleasesTask> _logger;
        private readonly AppReleasesProvider _appReleasesProvider;

        private bool _runOnce = false;
        private Timer _timer;

        public AppReleasesTask(
            ILogger<AppReleasesTask> logger,
            AppReleasesProvider appReleasesProvider
            )
        {
            _logger = logger;
            _appReleasesProvider = appReleasesProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            if (!_runOnce)
            {
                _appReleasesProvider.GetLatestVersionAsync().Wait(stoppingToken);
                _runOnce = true;
            }

            _timer = new Timer(
                DoWork, 
                null, 
                TimeSpan.Zero,
                TimeSpan.FromHours(1)
                );

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            _ = _appReleasesProvider.GetLatestVersionAsync();
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