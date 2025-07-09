namespace TicketsToSky.Parser.Models.FlightModels;

using System.Text.Json.Serialization;
using System.Collections.Generic;
using TicketsToSky.Parser.Models.OtherModels;

public class Segment
{
    [JsonPropertyName("flight")]
    public List<FlightDetails> Flight { get; set; } = [];

    [JsonPropertyName("rating")]
    public Rating? Rating { get; set; }
}