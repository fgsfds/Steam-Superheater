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
            _logger.LogInformation("NewsListUpdateTask is running");

            if (!_runOnce)
            {
                _newsProvider.CreateNewsList().Wait(stoppingToken);
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
            _logger.LogInformation("NewsListUpdateTask is working");
            _ = _newsProvider.CreateNewsList();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NewsListUpdateTask is stopping");
            _timer.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}