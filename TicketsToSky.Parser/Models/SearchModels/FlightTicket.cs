using TicketsToSky.Parser.Models.FlightModels;

namespace TicketsToSky.Parser.Models.SearchModels;

public class FlightTicket
{
    public required string Sign { get; set; }
    public int Price { get; set; }
    public required string Currency { get; set; }
    public required string DepartureAirport { get; set; }
    public required string ArrivalAirport { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public int TotalDuration { get; set; }
    public int StopsCount { get; set; }
    public required List<string> StopsAirports { get; set; }
    public required List<Transfer> Transfers { get; set; }
    public required List<Flight> Flights { get; set; }
    public int BaggageAmount { get; set; }
    public required string BaggageInfo { get; set; }
    public int HandbagsAmount { get; set; }
    public required string HandbagsInfo { get; set; }
    public required string ValidatingCarrier { get; set; }
    public bool IsDirect { get; set; }
    public required string TariffCode { get; set; }
    public required string Url { get; set; }
    public required string LinkToBuy { get; set; }
}