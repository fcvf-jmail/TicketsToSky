namespace TicketsToSky.Parser.Models.OtherModels;

using System.Text.Json.Serialization;

public class Rating
{
    [JsonPropertyName("total")]
    public double Total { get; set; }
}