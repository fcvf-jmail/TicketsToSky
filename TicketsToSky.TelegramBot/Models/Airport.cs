using System.Text.Json.Serialization;

namespace TicketsToSky.TelegramBot.Models
{
    public class Airport
    {
        public string Slug { get; set; }
        public string Subtitle { get; set; }
        public string Title { get; set; }
    }
}