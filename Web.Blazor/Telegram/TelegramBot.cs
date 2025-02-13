using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;

namespace Web.Blazor.Telegram;

public sealed class TelegramBot
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramBot> _logger;

    private readonly string _chatId = Environment.GetEnvironmentVariable("TgChatId")!;
    private readonly string _token = Environment.GetEnvironmentVariable("TgToken")!;
    private readonly string _apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

    public TelegramBot(
        HttpClient httpClient,
        ILogger<TelegramBot> logger
        )
    {
        _httpClient = httpClient;
        _logger = logger;
        _bot = new TelegramBotClient(_token);
    }

    public async Task StartAsync()
    {
        _ = await _bot.GetMeAsync();

        var task = new Task(async () =>
        {
            var updates = await _bot.GetUpdatesAsync();

            while (true)
            {
                var count = updates.Count();

                if (count > 0)
                {
                    _logger.LogInformation($"Got {count} telegram updates");

                    foreach (var update in updates)
                    {
                        if (update.Message is not null)
                        {
                            _logger.LogInformation($"Got message {update.Message.Text}");

                            if (!update.Message?.From?.Id.ToString().Equals(_chatId) ?? false)
                            {
                                await SendMessageAsync(
                                    "You are not supposed to be here",
                                    update.Message?.From?.Id.ToString()
                                    );

                                continue;
                            }

                            if (update.Message!.Text!.Equals("Ping", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Pong");
                                await SendMessageAsync("Pong");
                            }
                            else if (update.Message!.Text!.Equals("Check", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Check message received");
                                _ = await _httpClient.PostAsJsonAsync("https://superheater.fgsfds.link/api/fixes/check", _apiPassword);
                            }
                        }
                    }
                }

                await Task.Delay(1000);

                var offset = updates.LastOrDefault() is null ? 0 : updates.Last().UpdateId + 1;
                updates = _bot.GetUpdates(offset);
            }
        });

        task.Start();
    }

    public async Task SendMessageAsync(string text, string? id = null)
    {
        id ??= _chatId;

        _ = await _bot.SendMessageAsync(
            id,
            text
            );
    }
}

