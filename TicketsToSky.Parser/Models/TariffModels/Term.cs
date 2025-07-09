namespace TicketsToSky.Parser.Models.TariffModels;

using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json;

public class Term
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("unified_price")]
    public int UnifiedPrice { get; set; }

    [JsonPropertyName("url")]
    public int Url { get; set; }

    [JsonPropertyName("transfer_terms")]
    public List<List<JsonElement>> TransferTerms { get; set; } = [];

    [JsonPropertyName("flight_additional_tariff_infos")]
    public List<List<TariffInfo>> FlightAdditionalTariffInfos { get; set; } = [];

    [JsonPropertyName("flights_baggage")]
    public List<List<JsonElement>> FlightsBaggage { get; set; } = [];

    [JsonPropertyName("flights_handbags")]
    public List<List<JsonElement>> FlightsHandbags { get; set; } = [];

    [JsonPropertyName("baggage_source")]
    public List<List<JsonElement>> BaggageSource { get; set; } = [];

    [JsonPropertyName("handbags_source")]
    public List<List<JsonElement>> HandbagsSource { get; set; } = [];
}