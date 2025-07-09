using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Services.InfrastructureServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class ApiClient(HttpClient httpClient, IRequestRetryHandler retryHandler) : IApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IRequestRetryHandler _retryHandler = retryHandler;

    public async Task<List<Location>?> GetAirportCodesAsync(string airportOrCityName)
    {
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.GetAsync($"https://autocomplete.travelpayouts.com/places2?term={airportOrCityName}&locale=ru&types[]=city&types[]=airport&max=7"));
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Location>>(result);
    }

    public async Task<Guid> GetSearchIdAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0)
    {
        object objectBody = new
        {
            segments = new[] { new { origin = departureCode, date = departureDate.ToString("yyyy-MM-dd"), destination = destinationCode } },
            passengers = new { adults = amountOfAdults, children = amountOfChildren, infants = amountOfInfants },
            trip_class = "Y",
            with_request = true,
            locale = "ru",
            lang = "ru",
            marker = "12814.Zz2109933655aa4e7ba8d76d42-12814.$1489",
            currency = "rub"
        };
        string stringBody = JsonSerializer.Serialize(objectBody);
        StringContent stringContent = new(stringBody, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.PostAsync("https://avia.loukoster.com/adaptors/chains/rt_search_native_format", stringContent));
        string result = await response.Content.ReadAsStringAsync();

        JsonNode? resultNode = JsonNode.Parse(result);
        string searchId = resultNode["search_id"].ToString();
        return new Guid(searchId);
    }

    public async Task<string> GetSearchResultsAsync(Guid searchId)
    {
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.GetAsync($"https://avia.loukoster.com/searches_results_united?uuid={searchId}&{new Random().NextDouble()}"));
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetAirportName(string airportCode)
    {
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.GetAsync($"https://autocomplete.travelpayouts.com/places2?term={airportCode}&locale=ru&types[]=city&types[]=airport&max=7"));
        return await response.Content.ReadAsStringAsync();
    }
}