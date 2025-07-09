namespace TicketsToSky.Parser.Models.FlightModels;

using System.Text.Json.Serialization;

public class FlightDetails
{
    [JsonPropertyName("aircraft")]
    public string Aircraft { get; set; } = string.Empty;

    [JsonPropertyName("arrival")]
    public string Arrival { get; set; } = string.Empty;

    [JsonPropertyName("arrival_date")]
    public string ArrivalDate { get; set; } = string.Empty;

    [JsonPropertyName("arrival_time")]
    public string ArrivalTime { get; set; } = string.Empty;

    [JsonPropertyName("arrival_timestamp")]
    public long ArrivalTimestamp { get; set; }

    [JsonPropertyName("delay")]
    public int Delay { get; set; }

    [JsonPropertyName("departure")]
    public string Departure { get; set; } = string.Empty;

    [JsonPropertyName("departure_date")]
    public string DepartureDate { get; set; } = string.Empty;

    [JsonPropertyName("departure_time")]
    public string DepartureTime { get; set; } = string.Empty;

    [JsonPropertyName("departure_timestamp")]
    public long DepartureTimestamp { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("equipment")]
    public string Equipment { get; set; } = string.Empty;

    [JsonPropertyName("local_arrival_timestamp")]
    public long LocalArrivalTimestamp { get; set; }

    [JsonPropertyName("local_departure_timestamp")]
    public long LocalDepartureTimestamp { get; set; }

    [JsonPropertyName("marketing_carrier")]
    public string MarketingCarrier { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("operating_carrier")]
    public string OperatingCarrier { get; set; } = string.Empty;

    [JsonPropertyName("operated_by")]
    public string OperatedBy { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("technical_stops")]
    public object TechnicalStops { get; set; } = string.Empty;

    [JsonPropertyName("trip_class")]
    public string TripClass { get; set; } = string.Empty;

    [JsonPropertyName("seats")]
    public int? Seats { get; set; }
}
