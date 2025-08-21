using System.Text.Json.Serialization;

namespace TicketsToSky.TelegramBot.Models
{
    public class Subscription
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("chatId")]
        public long ChatId { get; set; }

        [JsonPropertyName("MaxPrice")]
        public int MaxPrice { get; set; }

        [JsonPropertyName("DepartureAirport")]
        public string DepartureAirport { get; set; } = string.Empty;

        [JsonPropertyName("ArrivalAirport")]
        public string ArrivalAirport { get; set; } = string.Empty;

        [JsonPropertyName("DepartureDate")]
        public string DepartureDate { get; set; } = string.Empty;

        [JsonPropertyName("MaxTransfersCount")]
        public int MaxTransfersCount { get; set; }

        [JsonPropertyName("MinBaggageAmount")]
        public int MinBaggageAmount { get; set; }

        [JsonPropertyName("MinHandbagsAmount")]
        public int MinHandbagsAmount { get; set; }
    }
}