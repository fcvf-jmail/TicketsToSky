using System.Text.Json.Serialization;
namespace TicketsToSky.Api.Models;

public class SubscriptionEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RabbitMqEventEnum Event { get; set; }
    public Guid Id { get; set; }
    public long ChatId { get; set; }
    public int MaxPrice { get; set; }
    public required string DepartureAirport { get; set; }
    public required string ArrivalAirport { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int MaxTransfersCount { get; set; }
    public int MinBaggageAmount { get; set; }
    public int MinHandbagsAmount { get; set; }
}