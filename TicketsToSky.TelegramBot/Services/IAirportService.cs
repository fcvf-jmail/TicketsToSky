using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services;

public interface IAirportService
{
    Task<List<Airport>> SearchAirportsAsync(string term);
}