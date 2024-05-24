using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ReceiverOptions _receiverOptions;

        private readonly string _chatId = Environment.GetEnvironmentVariable("TgChatId")!;
        private readonly string _token = Environment.GetEnvironmentVariable("TgToken")!;

        public TelegramBot()
        {
            _botClient = new TelegramBotClient(_token);

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

                            if (update.Message!.Text!.Equals("Ping", StringComparison.InvariantCultureIgnoreCase))
                            {
                                await SendMessageAsync(
                                    "Pong"
                                    ).ConfigureAwait(false);
                            }

                            return;
                        }
                }
            }
            catch (Exception ex)
            {
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

            await SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
