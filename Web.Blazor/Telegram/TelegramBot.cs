using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Web.Blazor.Telegram
{
    public sealed class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ReceiverOptions _receiverOptions;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TelegramBot> _logger;

        private readonly string _chatId = Environment.GetEnvironmentVariable("TgChatId")!;
        private readonly string _token = Environment.GetEnvironmentVariable("TgToken")!;
        private readonly string _apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

        public TelegramBot(HttpClient httpClient)
        {
            _botClient = new TelegramBotClient(_token);
            _httpClient = httpClient;

            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message],
                ThrowPendingUpdates = true,
            };
        }

        public async Task StartAsync()
        {
            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _botClient.GetMeAsync().ConfigureAwait(false);

            await SendMessageAsync("Server started").ConfigureAwait(false);
        }

        public async Task SendMessageAsync(string text, string? id = null)
        {
            id ??= _chatId;

            await _botClient.SendTextMessageAsync(
                id,
                text
                ).ConfigureAwait(false);
        }

        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Got update of type {update.Type.ToString()}");

                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            if (update.Message!.From!.Id.ToString() != _chatId)
                            {
                                await SendMessageAsync(
                                    "You are not supposed to be here",
                                    update.Message!.From!.Id.ToString()
                                    ).ConfigureAwait(false);
                            }

                            if (update.Message!.Text!.Equals("Ping", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Pong");
                                await SendMessageAsync("Pong").ConfigureAwait(false);
                            }
                            else if (update.Message!.Text!.Equals("Check", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Check message received");
                                await _httpClient.PostAsJsonAsync($"https://superheater.fgsfds.link/api/fixes/check", _apiPassword, cancellationToken).ConfigureAwait(false);
                            }

                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical telegram error");
                await SendMessageAsync(ex.ToString()).ConfigureAwait(false);
            }
        }

        private async Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var message = error switch
            {
                ApiRequestException apiRequestException => $"""
                Telegram API Error:

                {apiRequestException.ErrorCode}

                {apiRequestException.Message}
                """,
                _ => error.ToString()
            };

            _logger.LogError(message);
            await SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
