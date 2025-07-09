namespace TicketsToSky.Parser.Models.FlightModels;

public class Transfer
{
    public required string FromAirport { get; set; }
    public required string FromAirportName { get; set; }
    public required string FromCityName { get; set; }
    public required string ToAirport { get; set; }
    public required string ToAirportName { get; set; }
    public required string ToCityName { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int DurationMinutes { get; set; }
}