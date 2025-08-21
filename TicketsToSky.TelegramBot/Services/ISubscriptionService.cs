using System.Collections.Generic;
using System.Threading.Tasks;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services
{
    public interface ISubscriptionService
    {
        Task CreateSubscriptionAsync(Subscription subscription);
        Task<Subscription> GetSubscriptionAsync(string id);
        Task UpdateSubscriptionAsync(Subscription subscription);
        Task DeleteSubscriptionAsync(string id);
        Task<List<Subscription>> GetUserSubscriptionsAsync(long chatId);
    }
}