using TicketsToSky.Parser.Models.LocationModels;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public interface IApiClient
{
    public Task<List<Location>?> GetAirportCodesAsync(string airportOrCityName);
    public Task<Guid> GetSearchIdAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0);
    public Task<string> GetSearchResultsAsync(Guid searchId);
    public Task<string> GetAirportName(string airportCode);
}