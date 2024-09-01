using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        _bot = new TelegramBotClient(_token, httpClient);
    }

    public async Task StartAsync()
    {
        var me = await _bot.GetMeAsync().ConfigureAwait(false);

        _bot.OnError += OnError;
        _bot.OnMessage += OnMessage;

        await SendMessageAsync("Server started").ConfigureAwait(false);
    }

    private async Task OnMessage(Message message, UpdateType type)
    {
        _logger.LogInformation("Got message");

        if (message.From!.Id.ToString() != _chatId)
        {
            await SendMessageAsync(
                "You are not supposed to be here",
                message!.From!.Id.ToString()
                ).ConfigureAwait(false);
        }

        if (message!.Text!.Equals("Ping", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Pong");
            await SendMessageAsync("Pong").ConfigureAwait(false);
        }
        else if (message!.Text!.Equals("Check", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Check message received");
            await _httpClient.PostAsJsonAsync($"https://superheater.fgsfds.link/api/fixes/check", _apiPassword).ConfigureAwait(false);
        }
    }

    private async Task OnError(Exception exception, HandleErrorSource source)
    {
        var message = exception switch
        {
            ApiRequestException apiRequestException => $"""
                Telegram API Error:

                {apiRequestException.ErrorCode}

                {apiRequestException.Message}
                """,
            _ => exception.ToString()
        };

        _logger.LogError(message);
        await SendMessageAsync(message).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string text, string? id = null)
    {
        id ??= _chatId;

        await _bot.SendTextMessageAsync(
            id,
            text
            ).ConfigureAwait(false);
    }
}

