using TicketsToSky.Parser.Models.LocationModels;
using TicketsToSky.Parser.Services.InfrastructureServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class ApiClient(HttpClient httpClient, IRequestRetryHandler retryHandler, IConfiguration configuration) : IApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IRequestRetryHandler _retryHandler = retryHandler;
    private readonly IConfiguration _configuration = configuration;
    private readonly string _cookieValue = $"_awt={configuration["Parser:AwtValue"]}";
    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", _cookieValue);
        if (content != null) request.Content = content;
        return request;
    }

    public async Task<List<Location>?> GetAirportCodesAsync(string airportOrCityName)
    {
        var request = CreateRequest(HttpMethod.Get, $"https://autocomplete.travelpayouts.com/places2?term={airportOrCityName}&locale=ru&types[]=city&types[]=airport&max=7");
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.SendAsync(request));
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
        var request = CreateRequest(HttpMethod.Post, "https://tickets-api.apistp.com/adaptors/chains/rt_search_native_format", stringContent);
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.SendAsync(request));
        string result = await response.Content.ReadAsStringAsync();

        JsonNode? resultNode = JsonNode.Parse(result);
        string searchId = resultNode?["search_id"]?.ToString() ?? throw new Exception("search_id not found in response");
        return new Guid(searchId);
    }

    public async Task<string> GetSearchResultsAsync(Guid searchId)
    {
        var request = CreateRequest(HttpMethod.Get, $"https://tickets-api.eu-north-1.apistp.com/searches_results_united?uuid={searchId}&{new Random().NextDouble()}");
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.SendAsync(request));
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetAirportName(string airportCode)
    {
        var request = CreateRequest(HttpMethod.Get, $"https://autocomplete.apistp.com/places2?term={airportCode}&locale=ru&types[]=city&types[]=airport&max=7");
        HttpResponseMessage response = await _retryHandler.ExecuteWithRetryAsync(() => _httpClient.SendAsync(request));
        return await response.Content.ReadAsStringAsync();
    }
}