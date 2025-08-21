using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services
{
    public class AirportService : IAirportService
    {
        private readonly HttpClient _httpClient;

        public AirportService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Airport>> SearchAirportsAsync(string term)
        {
            var url = $"https://autocomplete.travelpayouts.com/places2?term={Uri.EscapeDataString(term)}&locale=ru&types[]=city&types[]=airport&max=7";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Airport>>(content) ?? new List<Airport>();
        }
    }
}