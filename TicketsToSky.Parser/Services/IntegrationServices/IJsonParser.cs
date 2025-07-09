using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public interface IJsonParser
{
    public bool IsFullAnswer(string jsonResponse);
    public IAsyncEnumerable<Proposal?> ParseFlightsAsync(string jsonString);
    public Task<List<Airport>> ParseAirportsAsync(string jsonString);
}