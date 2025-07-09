namespace TicketsToSky.Parser.Models.LocationModels;

using System.Text.Json.Serialization;

public class Coordinates
{
    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }
}