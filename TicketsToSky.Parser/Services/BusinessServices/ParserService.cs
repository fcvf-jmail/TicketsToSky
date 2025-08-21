using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Services.IntegrationServices;

namespace TicketsToSky.Parser.Services.BusinessServices;

public class ParserService(IPlaywrightService playwrightService, IJsonParser jsonParser, ITicketConverter ticketConverter) : IParserService
{
    private readonly IPlaywrightService _playwrightService = playwrightService;
    private readonly IJsonParser _jsonParser = jsonParser;
    private readonly ITicketConverter _ticketConverter = ticketConverter;

    public async Task<List<FlightTicket>> GetTicketsAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0)
    {
        List<FlightTicket> allFlightTickets = [];
        (List<string> responseBodies, Guid searchId) = await _playwrightService.GetSearchResultsAsync(departureCode, departureDate, destinationCode, amountOfAdults, amountOfChildren, amountOfInfants);

        foreach (string responseBody in responseBodies)
        {
            IAsyncEnumerable<Proposal?> proposals = _jsonParser.ParseFlightsAsync(responseBody);
            List<Airport> airports = await _jsonParser.ParseAirportsAsync(responseBody);

            List<FlightTicket> flightTickets = await _ticketConverter.ConvertProposalsToFlightTickets(proposals.ToBlockingEnumerable(), airports, searchId);
            allFlightTickets.AddRange(flightTickets);
        }

        return allFlightTickets;
    }
}