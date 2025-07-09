namespace TicketsToSky.Parser.Models.FlightModels;

public class Flight
{
    public string DepartureAirport { get; set; } = string.Empty;
    public string DepartureAirportName { get; set; } = string.Empty;
    public string DepartureCityName { get; set; } = string.Empty;
    public string ArrivalAirport { get; set; } = string.Empty;
    public string ArrivalAirportName { get; set; } = string.Empty;
    public string ArrivalCityName { get; set; } = string.Empty;
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
}