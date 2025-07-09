namespace TicketsToSky.Parser.Models.SearchModels;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class ProposalWrapper
{
    [JsonPropertyName("proposals")]
    public List<Proposal> Proposals { get; set; } = [];
}