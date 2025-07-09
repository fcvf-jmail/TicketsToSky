namespace TicketsToSky.Parser.Models.SearchModels;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class FlightData
{
    [JsonPropertyName("proposals")]
    public List<ProposalWrapper> Proposals { get; set; } = [];
    [JsonPropertyName("airports")]
    public string Airports { get; set; }
}