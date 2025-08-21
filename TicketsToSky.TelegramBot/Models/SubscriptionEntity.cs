using System.ComponentModel.DataAnnotations;

namespace TicketsToSky.TelegramBot.Models
{
    public class SubscriptionEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public long ChatId { get; set; }
        public string DepartureAirport { get; set; } = string.Empty;
        public string ArrivalAirport { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public int? MaxPrice { get; set; }
        public int? MaxTransfersCount { get; set; }
        public int? MinBaggageAmount { get; set; }
        public int? MinHandbagsAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
