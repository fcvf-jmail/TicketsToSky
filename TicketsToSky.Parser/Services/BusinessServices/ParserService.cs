using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Services.IntegrationServices;

namespace TicketsToSky.Parser.Services.BusinessServices;

public class ParserService(IApiClient apiClient, IJsonParser jsonParser, ITicketConverter ticketConverter) : IParserService
{
    private readonly IApiClient _apiClient = apiClient;
    private readonly IJsonParser _jsonParser = jsonParser;
    private readonly ITicketConverter _ticketConverter = ticketConverter;

    public async Task<List<Location>?> GetAirportCodesAsync(string airportOrCityName)
    {
        return await _apiClient.GetAirportCodesAsync(airportOrCityName);
    }

    public async Task<Guid> GetSearchIdAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0)
    {
        return await _apiClient.GetSearchIdAsync(departureCode, departureDate, destinationCode, amountOfAdults, amountOfChildren, amountOfInfants);
    }

    public async Task<List<FlightTicket>> GetTicketsAsync(Guid searchId)
    {
        List<FlightTicket> allFlightTickets = [];
        bool isFullAnswer = false;

        while (!isFullAnswer)
        {
            string result = await _apiClient.GetSearchResultsAsync(searchId);
            isFullAnswer = _jsonParser.IsFullAnswer(result);
            IAsyncEnumerable<Proposal?> proposals = _jsonParser.ParseFlightsAsync(result);
            List<Airport> airports = await _jsonParser.ParseAirportsAsync(result);

            List<FlightTicket> flightTickets = await _ticketConverter.ConvertProposalsToFlightTickets(proposals.ToBlockingEnumerable(), airports, searchId);
            allFlightTickets.AddRange(flightTickets);
        }

        return allFlightTickets;
    }
}