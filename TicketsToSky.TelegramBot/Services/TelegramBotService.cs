using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TicketsToSky.TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        public TelegramBotClient Client { get; }

        public TelegramBotService(IConfiguration configuration)
        {
            var botToken = "7208238793:AAHucASIYY9F0m6a-Ux7ScYfKBRgQupWDvo";
            // var botToken = configuration["BotToken"] ?? throw new ArgumentNullException("BotToken is not configured");
            Client = new TelegramBotClient(botToken);
        }

        public async Task StartReceiving(IUpdateHandler updateHandler)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };
            await Client.ReceiveAsync(updateHandler, receiverOptions);
        }
    }
}