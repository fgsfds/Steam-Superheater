using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Tasks
{
    public sealed class NewsListUpdateTask : IHostedService, IDisposable
    {
        private readonly ILogger<NewsListUpdateTask> _logger;
        private readonly NewsProvider _newsProvider;

        private bool _runOnce = false;
        private Timer _timer;

        public NewsListUpdateTask(
            ILogger<NewsListUpdateTask> logger,
            NewsProvider newsProvider
            )
        {
            _logger = logger;
            _newsProvider = newsProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            if (!_runOnce)
            {
                _newsProvider.CreateNewsListAsync().Wait(stoppingToken);
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
            _ = _newsProvider.CreateNewsListAsync();
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