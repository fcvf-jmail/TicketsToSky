namespace TicketsToSky.Api.Models;

public class SubscriptionDto
{
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