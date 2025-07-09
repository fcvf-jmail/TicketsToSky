namespace TicketsToSky.Parser.Services.BusinessServices;

using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;

public interface IParserService
{
    public Task<List<Location>?> GetAirportCodesAsync(string airportOrCityName);
    public Task<Guid> GetSearchIdAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0);
    public Task<List<FlightTicket>> GetTicketsAsync(Guid searchId);
}