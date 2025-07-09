namespace TicketsToSky.Parser.Models.TariffModels;

using System.Text.Json.Serialization;

public class TariffInfo
{
    [JsonPropertyName("return_before_flight")]
    public TariffOption? ReturnBeforeFlight { get; set; }

    [JsonPropertyName("return_after_flight")]
    public TariffOption? ReturnAfterFlight { get; set; }

    [JsonPropertyName("change_before_flight")]
    public TariffOption? ChangeBeforeFlight { get; set; }

    [JsonPropertyName("change_after_flight")]
    public TariffOption? ChangeAfterFlight { get; set; }
}