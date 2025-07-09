namespace TicketsToSky.Parser.Models.TariffModels;

using System.Text.Json.Serialization;
using TicketsToSky.Parser.Models.OtherModels;

public class TariffOption
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("penalty")]
    public Penalty? Penalty { get; set; }
}