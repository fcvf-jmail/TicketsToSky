namespace TicketsToSky.Parser.Models.LocationModels;

using System.Text.Json.Serialization;
using TicketsToSky.Parser.Models.OtherModels;

public class Location
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("country_name")]
    public string CountryName { get; set; } = string.Empty;

    [JsonPropertyName("state_code")]
    public string StateCode { get; set; } = string.Empty;

    [JsonPropertyName("Coordinates")]
    public Coordinates? Coordinates { get; set; }

    [JsonPropertyName("index_strings")]
    public List<string> IndexStrings { get; set; } = [];

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("cases")]
    public Cases? Cases { get; set; }

    [JsonPropertyName("country_cases")]
    public Cases? CountryCases { get; set; }

    [JsonPropertyName("main_airport_name")]
    public string MainAirportName { get; set; } = string.Empty;

    // Для аэропортов
    [JsonPropertyName("city_code")]
    public string CityCode { get; set; } = string.Empty;

    [JsonPropertyName("city_name")]
    public string CityName { get; set; } = string.Empty;

    [JsonPropertyName("city_cases")]
    public Cases? CityCases { get; set; }
}
