namespace TicketsToSky.Parser.Services.IntegrationServices;

public interface IPlaywrightService
{
    public Task<(List<string> responseBodies, Guid searchId)> GetSearchResultsAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0);
}