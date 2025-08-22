using System.Net.Http.Json;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services;

public class AirportService(HttpClient httpClient) : IAirportService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<Airport>> SearchAirportsAsync(string term)
    {
        var response = await _httpClient.GetFromJsonAsync<List<Airport>>($"https://suggest.apistp.com/search?service=aviasales&term={Uri.EscapeDataString(term)}&locale=ru");
        return response ?? [];
    }
}