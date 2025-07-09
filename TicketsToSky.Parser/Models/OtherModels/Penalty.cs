namespace TicketsToSky.Parser.Models.OtherModels;

using System.Text.Json.Serialization;

public class Penalty
{
    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; set; } = string.Empty;
}