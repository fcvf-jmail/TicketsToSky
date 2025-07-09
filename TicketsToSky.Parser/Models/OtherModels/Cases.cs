namespace TicketsToSky.Parser.Models.OtherModels;

using System.Text.Json.Serialization;

public class Cases
{
    [JsonPropertyName("vi")]
    public string Vi { get; set; } = string.Empty;

    [JsonPropertyName("tv")]
    public string Tv { get; set; } = string.Empty;

    [JsonPropertyName("su")]
    public string Su { get; set; } = string.Empty;

    [JsonPropertyName("ro")]
    public string Ro { get; set; } = string.Empty;

    [JsonPropertyName("pr")]
    public string Pr { get; set; } = string.Empty;

    [JsonPropertyName("da")]
    public string Da { get; set; } = string.Empty;
}
