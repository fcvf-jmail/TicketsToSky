using Microsoft.EntityFrameworkCore;
using TicketsToSky.TelegramBot.Data;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services
{
    public class SubscriptionStorageService(UserStateDbContext db)
    {
        private readonly UserStateDbContext _db = db;

        public async Task<List<SubscriptionEntity>> GetUserSubscriptionsAsync(long chatId)
        {
            return await _db.Set<SubscriptionEntity>().Where(x => x.ChatId == chatId).ToListAsync();
        }

        public async Task<SubscriptionEntity?> GetSubscriptionAsync(string id)
        {
            return await _db.Set<SubscriptionEntity>().FindAsync(id);
        }

        public async Task AddOrUpdateAsync(SubscriptionEntity entity)
        {
            var exists = await _db.Set<SubscriptionEntity>().AnyAsync(x => x.Id == entity.Id);
            if (!exists)
                _db.Set<SubscriptionEntity>().Add(entity);
            else
                _db.Set<SubscriptionEntity>().Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _db.Set<SubscriptionEntity>().FindAsync(id);
            if (entity != null)
            {
                _db.Set<SubscriptionEntity>().Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}
