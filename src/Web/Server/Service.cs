//using Common.Entities.Fixes;
//using System.Collections.Immutable;
//using System.Text.Json;

//namespace SuperheaterAPI;

//public class TimedHostedService : IHostedService, IDisposable
//{
//    private const string AddonsJson = "https://s3.timeweb.cloud/b70f50a9-files/superheater/fixes.json";

//    private readonly ILogger<TimedHostedService> _logger;
//    private readonly HttpClient _httpClient;

//    private ImmutableList<FixesList>? _fixesList;
//    private Timer? _timer = null;

//    public TimedHostedService(ILogger<TimedHostedService> logger, HttpClient httpClient)
//    {
//        _logger = logger;
//        _httpClient = httpClient;
//    }

//    public Task StartAsync(CancellationToken stoppingToken)
//    {
//        if (_fixesList is null)
//        {
//            _logger.LogInformation("Creating fixes list cache");

//            var list = _httpClient.GetStringAsync(AddonsJson, stoppingToken).Result;

//            var fixesList = JsonSerializer.Deserialize(list, FixesListContext.Default.ListFixesList);

//            _fixesList = [.. fixesList];
//        }

//        _timer = new Timer(
//            DoWork, 
//            null, 
//            TimeSpan.Zero,
//            TimeSpan.FromMinutes(5)
//            );

//        return Task.CompletedTask;
//    }

//    private void DoWork(object? state)
//    {
//        _logger.LogInformation("Updating fixes list cache");
//    }

//    public Task StopAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Timed Hosted Service is stopping.");

//        _timer?.Change(Timeout.Infinite, 0);

//        return Task.CompletedTask;
//    }

//    public void Dispose()
//    {
//        _timer?.Dispose();
//    }
//}