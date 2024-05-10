using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Tasks
{
    public sealed class FixesListUpdateTask : IHostedService, IDisposable
    {
        private readonly ILogger<FixesListUpdateTask> _logger;
        private readonly FixesProvider _fixesProvider;

        private bool _runOnce = false;
        private Timer _timer;

        public FixesListUpdateTask(
            ILogger<FixesListUpdateTask> logger,
            FixesProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            if (!_runOnce)
            {
                _fixesProvider.CreateFixesListAsync().Wait(stoppingToken);
                _runOnce = true;
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
            _ = _fixesProvider.CreateFixesListAsync();
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