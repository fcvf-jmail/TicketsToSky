using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services;

public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(Subscription subscription);
}