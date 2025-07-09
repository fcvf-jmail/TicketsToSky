using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Models.SearchModels;

namespace TicketsToSky.Parser.Services.BusinessServices;

public interface ITicketConverter
{
    public Task<List<FlightTicket>> ConvertProposalsToFlightTickets(IEnumerable<Proposal?> proposals, List<Airport> airports, Guid searchId);
}