using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Tasks
{
    public sealed class AppReleasesTask : IHostedService, IDisposable
    {
        private readonly ILogger<AppReleasesTask> _logger;
        private readonly AppReleasesProvider _fixesProvider;

        private bool _runOnce = false;
        private Timer _timer;

        public AppReleasesTask(
            ILogger<AppReleasesTask> logger,
            AppReleasesProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            if (!_runOnce)
            {
                _fixesProvider.GetLatestVersionAsync().Wait(stoppingToken);
                _runOnce = true;
                return Task.CompletedTask;
            }

            _timer = new Timer(
                DoWork, 
                null, 
                TimeSpan.Zero,
                TimeSpan.FromMinutes(5)
                );

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            _ = _fixesProvider.GetLatestVersionAsync();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}