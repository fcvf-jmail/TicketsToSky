using System.Text.Json;
using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Models.FlightModels;
using TicketsToSky.Parser.Models.TariffModels;
using System.Threading.Tasks;
using TicketsToSky.Parser.Models.LocationModels;
using System.Security.Cryptography.X509Certificates;

namespace TicketsToSky.Parser.Services.BusinessServices;

public class TicketConverter : ITicketConverter
{

    public async Task<List<FlightTicket>> ConvertProposalsToFlightTickets(IEnumerable<Proposal> proposals, List<Airport> airports, Guid searchId)
    {
        List<FlightTicket> flightTickets = [];

        foreach (Proposal proposal in proposals)
        {
            IEnumerable<FlightTicket> ticketsFromProposal = await ConvertToFlightTickets(proposal, airports, searchId);
            flightTickets.AddRange(ticketsFromProposal);
        }

        return flightTickets;
    }

    private async Task<IEnumerable<FlightTicket>> ConvertToFlightTickets(Proposal proposal, List<Airport> airports, Guid searchId)
    {
        List<Transfer> transfers = await ExtractTransfers(proposal, airports);
        List<Flight> flights = await ExtractFlights(proposal, airports);
        (string departureAirport, string arrivalAirport, DateTime departureDateTime, DateTime arrivalDateTime) = GetFlightEndpoints(proposal);

        int totalDuration = proposal.TotalDuration;
        int stopsCount = proposal.MaxStops;
        List<string> stopsAirports = proposal.StopsAirports;

        List<FlightTicket> flightTickets = [];

        foreach (var xterm in proposal.Xterms)
        {
            foreach (var tariff in xterm.Value)
            {
                var (baggageAmount, baggageInfo) = ExtractBaggageInfo(tariff.Value);
                var (handbagsAmount, handbagsInfo) = ExtractHandbagsInfo(tariff.Value);

                flightTickets.Add(new()
                {
                    Sign = proposal.Sign,
                    Price = tariff.Value.Price,
                    Currency = tariff.Value.Currency,
                    DepartureAirport = departureAirport,
                    ArrivalAirport = arrivalAirport,
                    DepartureDateTime = departureDateTime,
                    ArrivalDateTime = arrivalDateTime,
                    TotalDuration = totalDuration,
                    StopsCount = stopsCount,
                    StopsAirports = stopsAirports,
                    Transfers = transfers,
                    Flights = flights,
                    BaggageAmount = baggageAmount,
                    BaggageInfo = baggageInfo,
                    HandbagsAmount = handbagsAmount,
                    HandbagsInfo = handbagsInfo,
                    ValidatingCarrier = proposal.ValidatingCarrier,
                    IsDirect = proposal.IsDirect,
                    TariffCode = tariff.Key,
                    Url = tariff.Value.Url.ToString(),
                    LinkToBuy = $"https://avia.loukoster.com/searches/{searchId}/clicks/{tariff.Value.Url}.html"
                });
            }
        }

        return flightTickets;
    }

    private async Task<List<Transfer>> ExtractTransfers(Proposal proposal, List<Airport> airports)
    {
        List<Transfer> transfers = [];
        Segment currentSegment = proposal.Segment.First();

        if (proposal.MaxStops > 0)
        {
            for (int i = 0; i < currentSegment.Flight.Count - 1; i++)
            {
                FlightDetails currentFlight = currentSegment.Flight[i];
                FlightDetails nextFlight = currentSegment.Flight[i + 1];

                DateTime startDateTime = DateTimeOffset.FromUnixTimeSeconds(currentFlight.LocalArrivalTimestamp).DateTime;
                DateTime endDateTime = DateTimeOffset.FromUnixTimeSeconds(nextFlight.LocalDepartureTimestamp).DateTime;
                int durationMinutes = (int)(endDateTime - startDateTime).TotalMinutes;

                Airport? fromAirport = airports.Find(airport => airport.Code == currentFlight.Arrival);
                Airport? toAirport = airports.Find(airport => airport.Code == nextFlight.Departure);

                transfers.Add(new Transfer
                {
                    FromAirport = currentFlight.Arrival,
                    FromAirportName = fromAirport.Name,
                    FromCityName = fromAirport.CityName,
                    ToAirport = nextFlight.Departure,
                    ToAirportName = toAirport.Name,
                    ToCityName = toAirport.CityName,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    DurationMinutes = durationMinutes
                });
            }
        }

        return transfers;
    }

    private async Task<List<Flight>> ExtractFlights(Proposal proposal, List<Airport> airports)
    {
        List<Flight> flights = [];
        foreach (Segment segment in proposal.Segment)
        {
            foreach (FlightDetails flightDetails in segment.Flight)
            {
                Airport departureAirport = airports.Find(airport => airport.Code == flightDetails.Departure);
                Airport arrivalAirport = airports.Find(airport => airport.Code == flightDetails.Arrival);

                flights.Add(new Flight
                {
                    DepartureAirport = flightDetails.Departure,
                    DepartureAirportName = departureAirport.Name,
                    DepartureCityName = departureAirport.CityName,
                    ArrivalAirport = flightDetails.Arrival,
                    ArrivalAirportName = arrivalAirport.Name,
                    ArrivalCityName = arrivalAirport.CityName,
                    DepartureDateTime = DateTimeOffset.FromUnixTimeSeconds(flightDetails.LocalDepartureTimestamp).DateTime,
                    ArrivalDateTime = DateTimeOffset.FromUnixTimeSeconds(flightDetails.LocalArrivalTimestamp).DateTime,
                    DurationMinutes = flightDetails.Duration,
                    FlightNumber = flightDetails.Number
                });
            }
        }
        return flights;
    }

    private static (string departureAirport, string arrivalAirport, DateTime departureDateTime, DateTime arrivalDateTime) GetFlightEndpoints(Proposal proposal)
    {
        Segment currentSegment = proposal.Segment.First();
        FlightDetails firstFlight = currentSegment.Flight.First();
        FlightDetails lastFlight = currentSegment.Flight.Last();

        string departureAirport = firstFlight.Departure;
        string arrivalAirport = lastFlight.Arrival;
        DateTime departureDateTime = DateTimeOffset.FromUnixTimeSeconds(firstFlight.LocalDepartureTimestamp).DateTime;
        DateTime arrivalDateTime = DateTimeOffset.FromUnixTimeSeconds(lastFlight.LocalArrivalTimestamp).DateTime;

        return (departureAirport, arrivalAirport, departureDateTime, arrivalDateTime);
    }

    private static (int baggageAmount, string baggageInfo) ExtractBaggageInfo(Term term)
    {
        int baggageAmount = 0;
        string baggageInfo = "Unknown";

        if (term.FlightsBaggage != null && term.FlightsBaggage.Count > 0 && term.FlightsBaggage[0].Count > 0)
        {
            JsonElement baggage = term.FlightsBaggage[0][0];
            if (baggage.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(baggage.ToString().Split("PC")[0], out baggageAmount)) baggageAmount = 0;
                baggageInfo = baggage.ToString();
            }
            else if (baggage.ValueKind == JsonValueKind.False)
            {
                baggageInfo = "No baggage";
            }
        }

        return (baggageAmount, baggageInfo);
    }

    private static (int handbagsAmount, string handbagsInfo) ExtractHandbagsInfo(Term term)
    {
        int handbagsAmount = 0;
        string handbagsInfo = "Unknown";

        if (term.FlightsHandbags != null && term.FlightsHandbags.Count > 0 && term.FlightsHandbags[0].Count > 0)
        {
            JsonElement handbags = term.FlightsHandbags[0][0];
            if (handbags.ValueKind == JsonValueKind.String)
            {
                if (!int.TryParse(handbags.ToString().Split("PC")[0], out handbagsAmount)) handbagsAmount = 0;
                handbagsInfo = handbags.ToString();
            }
            else if (handbags.ValueKind == JsonValueKind.False)
            {
                handbagsInfo = "No handbags";
            }
        }

        return (handbagsAmount, handbagsInfo);
    }
}