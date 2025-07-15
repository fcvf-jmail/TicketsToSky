namespace TicketsToSky.Api.Models;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public required long ChatId { get; set; }
    public required int MaxPrice { get; set; }
    public required string DepartureAirport { get; set; }
    public required string ArrivalAirport { get; set; }
    public required DateOnly DepartureDate { get; set; }
    public required int MaxTransfersCount { get; set; }
    public required int MinBaggageAmount { get; set; }
    public required int MinHandbagsAmount { get; set; }
}