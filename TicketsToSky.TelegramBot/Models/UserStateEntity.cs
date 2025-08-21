using System.ComponentModel.DataAnnotations;

namespace TicketsToSky.TelegramBot.Models
{
    public class UserStateEntity
    {
        [Key]
        public long ChatId { get; set; }
        public string? StateJson { get; set; } // JSON сериализация SubscriptionState
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
