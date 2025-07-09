namespace TicketsToSky.Parser.Models.SearchModels;

using System.Text.Json.Serialization;
using System.Collections.Generic;
using TicketsToSky.Parser.Models.FlightModels;
using TicketsToSky.Parser.Models.TariffModels;

public class Proposal
{
    [JsonPropertyName("terms")]
    public Dictionary<string, Term> Terms { get; set; } = [];

    [JsonPropertyName("xterms")]
    public Dictionary<string, Dictionary<string, Term>> Xterms { get; set; } = [];

    [JsonPropertyName("segment")]
    public List<Segment> Segment { get; set; } = [];

    [JsonPropertyName("total_duration")]
    public int TotalDuration { get; set; }

    [JsonPropertyName("stops_airports")]
    public List<string> StopsAirports { get; set; } = [];

    [JsonPropertyName("is_charter")]
    public bool IsCharter { get; set; }

    [JsonPropertyName("max_stops")]
    public int MaxStops { get; set; }

    [JsonPropertyName("max_stop_duration")]
    public int MaxStopDuration { get; set; }

    [JsonPropertyName("carriers")]
    public List<string> Carriers { get; set; } = [];

    [JsonPropertyName("segment_durations")]
    public List<int> SegmentDurations { get; set; } = [];

    [JsonPropertyName("segments_time")]
    public List<List<long>> SegmentsTime { get; set; } = [];

    [JsonPropertyName("segments_airports")]
    public List<List<string>> SegmentsAirports { get; set; } = [];

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;

    [JsonPropertyName("is_direct")]
    public bool IsDirect { get; set; }

    [JsonPropertyName("flight_weight")]
    public double FlightWeight { get; set; }

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    [JsonPropertyName("segments_rating")]
    public double SegmentsRating { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("validating_carrier")]
    public string ValidatingCarrier { get; set; } = string.Empty;
}