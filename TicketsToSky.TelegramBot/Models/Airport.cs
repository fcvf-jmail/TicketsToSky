using System.Text.Json.Serialization;

namespace TicketsToSky.TelegramBot.Models
{
    public class Airport
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("city_name")]
        public string? CityName { get; set; }

        [JsonPropertyName("country_name")]
        public string CountryName { get; set; } = string.Empty;
    }
}