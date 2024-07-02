using Web.Blazor.Providers;

namespace Web.Blazor.Tasks
{
    public sealed class FileCheckerTask : IHostedService, IDisposable
    {
        private readonly FixesProvider _fixesProvider;

        private Timer _timer;

        public FileCheckerTask(
            FixesProvider fixesProvider
            )
        {
            _fixesProvider = fixesProvider;
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
            _ = _fixesProvider.CheckFixesAsync();
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