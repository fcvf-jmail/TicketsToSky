using System.Text.Json.Serialization;
namespace TicketsToSky.Parser.Models.LocationModels;


public class Airport
{
    public string Code { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("city")]
    public required string CityName { get; set; }
}