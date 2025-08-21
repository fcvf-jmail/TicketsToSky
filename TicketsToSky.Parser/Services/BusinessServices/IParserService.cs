namespace TicketsToSky.Parser.Services.BusinessServices;

using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;

public interface IParserService
{
    public Task<List<FlightTicket>> GetTicketsAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0);
}