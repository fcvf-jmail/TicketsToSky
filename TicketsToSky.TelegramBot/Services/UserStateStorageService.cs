using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketsToSky.TelegramBot.Data;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services
{
    public class UserStateStorageService(UserStateDbContext db)
    {
        private readonly UserStateDbContext _db = db;

        public async Task<SubscriptionState?> GetStateAsync(long chatId)
        {
            var entity = await _db.UserStates.FindAsync(chatId);
            if (entity?.StateJson == null) return null;
            return JsonSerializer.Deserialize<SubscriptionState>(entity.StateJson);
        }

        public async Task SaveStateAsync(long chatId, SubscriptionState state)
        {
            var json = JsonSerializer.Serialize(state);
            var entity = await _db.UserStates.FindAsync(chatId);
            if (entity == null)
            {
                entity = new UserStateEntity { ChatId = chatId, StateJson = json, UpdatedAt = DateTime.UtcNow };
                _db.UserStates.Add(entity);
            }
            else
            {
                entity.StateJson = json;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
        }
    }
}
