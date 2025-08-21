using Telegram.Bot;

namespace TicketsToSky.TelegramBot.Services
{
    public interface ITelegramBotService
    {
        TelegramBotClient Client { get; }
    }
}