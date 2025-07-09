using System.Text;
using System.Text.Json;
using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class JsonParser : IJsonParser
{
    public bool IsFullAnswer(string jsonResponse) => jsonResponse.Contains("{\"search_id\":\"");

    public async IAsyncEnumerable<Proposal?> ParseFlightsAsync(string jsonString)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonString));
        using JsonDocument document = await JsonDocument.ParseAsync(stream);

        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON root must be an array");

        foreach (JsonElement wrapperElement in root.EnumerateArray())
        {

            if (!wrapperElement.TryGetProperty("proposals", out JsonElement proposalsArray)) continue;
            foreach (JsonElement proposalElement in proposalsArray.EnumerateArray())
            {
                Proposal? proposal = null;
                try
                {
                    proposal = JsonSerializer.Deserialize<Proposal>(proposalElement.GetRawText());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing proposal: {ex.Message}");
                    Console.WriteLine(proposalElement.GetRawText());
                }
                yield return proposal;
            }
        }
    }
    public async Task<List<Airport>> ParseAirportsAsync(string jsonString)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(jsonString));
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON root must be an array");
        HashSet<Airport> allAirports = [];

        foreach (JsonElement wrapperElement in root.EnumerateArray())
        {
            wrapperElement.TryGetProperty("airports", out JsonElement airportsObject);
            if (airportsObject.ValueKind == JsonValueKind.Undefined) continue;
            foreach (JsonProperty airportObject in airportsObject.EnumerateObject())
            {
                Airport airport = JsonSerializer.Deserialize<Airport>(airportObject.Value);
                airport.Code = airportObject.Name;
                allAirports.Add(airport);
            }
        }

        return [.. allAirports];
    }
}