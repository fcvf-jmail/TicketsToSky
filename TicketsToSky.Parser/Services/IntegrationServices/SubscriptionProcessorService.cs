using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Models.SubscriptionModels;
using TicketsToSky.Parser.Services.BusinessServices;
using TicketsToSky.Parser.Services.InfrastructureServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class SubscriptionProcessorService(ILogger<SubscriptionProcessorService> logger, IHttpClientFactory httpClientFactory, IParserService parserService, ICacheService cacheService, QueueListenerService queueListenerService, IConfiguration configuration, IRequestRetryHandler requestRetryHandler) : IHostedService, IDisposable
{
    private readonly ILogger<SubscriptionProcessorService> _logger = logger;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SubscriptionsClient");
    private readonly IParserService _parserService = parserService;
    private readonly ICacheService _cacheService = cacheService;
    private readonly QueueListenerService _queueListenerService = queueListenerService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IRequestRetryHandler _requestRetryHandler = requestRetryHandler;
    private Timer? _timer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubscriptionProcessorService starting.");
        await FetchAndCacheSubscriptionsAsync(cancellationToken);
        string searchIntervalSecondsString = _configuration["Parser:SearchIntervalSeconds"] ?? throw new ArgumentNullException("Parser:SearchIntervalSeconds");
        int searchIntervalSeconds = int.Parse(searchIntervalSecondsString);
        _timer = new Timer(async state => await ProcessCachedSubscriptionsAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(searchIntervalSeconds));
    }

    private async Task FetchAndCacheSubscriptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching subscriptions from API");
            HttpResponseMessage response = await _requestRetryHandler.ExecuteWithRetryAsync(() => _httpClient.GetAsync("http://localhost:5148/api/v1/subscriptions/", cancellationToken));
            response.EnsureSuccessStatusCode();

            List<SubscriptionEvent>? subscriptions = await response.Content.ReadFromJsonAsync<List<SubscriptionEvent>>(cancellationToken);

            if (subscriptions == null || subscriptions.Count == 0)
            {
                _logger.LogWarning("No subscriptions received from API");
                return;
            }

            _logger.LogInformation($"Received {subscriptions.Count} subscriptions from API");

            IEnumerable<string> subscriptionKeys = await _cacheService.GetKeysAsync("Subscription:*");
            _logger.LogInformation("Deleting old cache with {oldSubscriptionsCount}", subscriptionKeys.Count());
            foreach (string subscriptionKey in subscriptionKeys) await _cacheService.RemoveAsync(subscriptionKey);

            foreach (SubscriptionEvent subscription in subscriptions)
            {
                await _cacheService.SetAsync($"Subscription:{subscription.Id}", subscription, null);
                _logger.LogInformation("Cached SubscriptionEvent for subscription {subscriptionId}", subscription.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching subscriptions from API: {ex.Message}");
        }
    }

    private async Task ProcessCachedSubscriptionsAsync()
    {
        try
        {
            _logger.LogInformation("Processing cached subscriptions.");

            IEnumerable<string> subscriptionKeys = await _cacheService.GetKeysAsync("Subscription:*");
            if (subscriptionKeys == null || !subscriptionKeys.Any())
            {
                _logger.LogWarning("No subscriptions found in cache.");
                return;
            }

            foreach (var key in subscriptionKeys)
            {
                SubscriptionEvent? subscription = await _cacheService.GetAsync<SubscriptionEvent>(key);
                if (subscription == null)
                {
                    _logger.LogWarning($"Subscription not found for key {key}.");
                    continue;
                }

                _logger.LogInformation($"Processing subscription {subscription.Id}");

                var departureLocations = await _parserService.GetAirportCodesAsync(subscription.DepartureAirport);
                if (departureLocations == null || !departureLocations.Any())
                {
                    _logger.LogWarning($"No departure airports found for {subscription.DepartureAirport}");
                    continue;
                }
                string departureCode = departureLocations.First().Code;

                var arrivalLocations = await _parserService.GetAirportCodesAsync(subscription.ArrivalAirport);
                if (arrivalLocations == null || !arrivalLocations.Any())
                {
                    _logger.LogWarning($"No arrival airports found for {subscription.ArrivalAirport}");
                    continue;
                }
                string arrivalCode = arrivalLocations.First().Code;

                Guid searchId = await _parserService.GetSearchIdAsync(departureCode, subscription.DepartureDate, arrivalCode, 1);

                List<FlightTicket> flightTickets = await _parserService.GetTicketsAsync(searchId);

                List<FlightTicket> filteredTickets = flightTickets
                    .Where(ticket => ticket.Price <= subscription.MaxPrice &&
                        ticket.StopsCount <= subscription.MaxTransfersCount &&
                        ticket.BaggageAmount >= subscription.MinBaggageAmount &&
                        ticket.HandbagsAmount >= subscription.MinHandbagsAmount)
                    .ToList();

                _logger.LogInformation($"Found {filteredTickets.Count} tickets for subscription {subscription.Id}");

                foreach (FlightTicket flightTicket in filteredTickets) await _queueListenerService.PublishFlightTicketAsync(flightTicket, subscription.ChatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing cached subscriptions: {ex.Message}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubscriptionProcessorService stopping.");
        await Task.Run(() => _timer?.Change(Timeout.Infinite, Timeout.Infinite), cancellationToken);
        _timer?.Dispose();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}