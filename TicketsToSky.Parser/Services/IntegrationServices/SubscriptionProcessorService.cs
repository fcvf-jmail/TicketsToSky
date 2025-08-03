using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Models.SubscriptionModels;
using TicketsToSky.Parser.Services.BusinessServices;
using TicketsToSky.Parser.Services.InfrastructureServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using TicketsToSky.Parser.Models.LocationModels;

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
    private readonly string _apiUrl = configuration["Parser:ApiUrl"] ?? throw new ArgumentNullException("Parser:ApiUrl", "API URL must be configured");
    private CancellationTokenSource? _cts;
    private Task? _processingTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] SubscriptionProcessorService starting.", DateTime.UtcNow);
        await FetchAndCacheSubscriptionsAsync(cancellationToken);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = Task.Run(() => RunProcessingLoopAsync(_cts.Token), cancellationToken);
    }

    private async Task RunProcessingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting new processing cycle at {Time}", DateTime.UtcNow);
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime iterationStart = DateTime.UtcNow;
                _logger.LogInformation("[{Time}] Processing iteration started.", iterationStart);
                await ProcessCachedSubscriptionsAsync(cancellationToken);
                DateTime iterationEnd = DateTime.UtcNow;
                _logger.LogInformation("[{Time}] Processing iteration finished. Duration: {Duration} сек.", iterationEnd, (iterationEnd - iterationStart).TotalSeconds);
                string searchIntervalSecondsString = _configuration["Parser:SearchIntervalSeconds"] ?? throw new ArgumentNullException("Parser:SearchIntervalSeconds");
                await Task.Delay(TimeSpan.FromSeconds(int.Parse(searchIntervalSecondsString)), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{Time}] Subscription processing loop cancelled", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] Error in subscription processing loop", DateTime.UtcNow);
        }
    }

    private async Task FetchAndCacheSubscriptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fetchStart = DateTime.UtcNow;
            _logger.LogInformation("[{Time}] Fetching subscriptions from API", fetchStart);
            HttpResponseMessage response = await _requestRetryHandler.ExecuteWithRetryAsync(() => _httpClient.GetAsync($"http://{_apiUrl}:5148/api/v1/subscriptions/", cancellationToken));
            response.EnsureSuccessStatusCode();

            List<SubscriptionEvent>? subscriptions = await response.Content.ReadFromJsonAsync<List<SubscriptionEvent>>(cancellationToken);

            if (subscriptions == null || subscriptions.Count == 0)
            {
                _logger.LogWarning("[{Time}] No subscriptions received from API", DateTime.UtcNow);
                return;
            }

            _logger.LogInformation("[{Time}] Received {Count} subscriptions from API", DateTime.UtcNow, subscriptions.Count);

            IEnumerable<string> subscriptionKeys = await _cacheService.GetKeysAsync("Subscription:*");
            _logger.LogInformation("[{Time}] Deleting old cache with {OldSubscriptionsCount}", DateTime.UtcNow, subscriptionKeys.Count());
            foreach (string subscriptionKey in subscriptionKeys)
            {
                await _cacheService.RemoveAsync(subscriptionKey);
                _logger.LogDebug("[{Time}] Removed cache key: {Key}", DateTime.UtcNow, subscriptionKey);
            }

            foreach (SubscriptionEvent subscription in subscriptions)
            {
                await _cacheService.SetAsync($"Subscription:{subscription.Id}", subscription, null);
                _logger.LogInformation("[{Time}] Cached SubscriptionEvent for subscription {SubscriptionId}", DateTime.UtcNow, subscription.Id);
            }
            DateTime fetchEnd = DateTime.UtcNow;
            _logger.LogInformation("[{Time}] Finished fetching and caching subscriptions. Duration: {Duration} сек.", fetchEnd, (fetchEnd - fetchStart).TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] Error fetching subscriptions from API", DateTime.UtcNow);
        }
    }

    private async Task ProcessCachedSubscriptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            DateTime processStart = DateTime.UtcNow;
            _logger.LogInformation("[{Time}] [START] Processing cached subscriptions.", processStart);

            IEnumerable<string> subscriptionKeys = await _cacheService.GetKeysAsync("Subscription:*");
            _logger.LogDebug("[{Time}] Found {Count} subscription keys in cache.", DateTime.UtcNow, subscriptionKeys?.Count() ?? 0);
            if (subscriptionKeys == null || !subscriptionKeys.Any())
            {
                _logger.LogWarning("[{Time}] No subscriptions found in cache.", DateTime.UtcNow);
                return;
            }

            foreach (var key in subscriptionKeys)
            {
                DateTime subStart = DateTime.UtcNow;
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(120));
                SubscriptionEvent? subscription = await _cacheService.GetAsync<SubscriptionEvent>(key);
                if (subscription == null)
                {
                    _logger.LogWarning("[{Time}] Subscription not found for key: {Key}", DateTime.UtcNow, key);
                    continue;
                }

                _logger.LogInformation("[{Time}] [SUBSCRIPTION] Processing subscription {Id} | Departure: {DepartureAirport} | Arrival: {ArrivalAirport} | Date: {DepartureDate:yyyy-MM-dd}", subStart, subscription.Id, subscription.DepartureAirport, subscription.ArrivalAirport, subscription.DepartureDate);

                var departureLocations = await _parserService.GetAirportCodesAsync(subscription.DepartureAirport);
                _logger.LogDebug("[{Time}] Departure airport locations: {Locations}", DateTime.UtcNow, departureLocations);
                if (departureLocations == null || departureLocations.Count == 0)
                {
                    _logger.LogWarning("[{Time}] Departure airport not found: {DepartureAirport}", DateTime.UtcNow, subscription.DepartureAirport);
                    continue;
                }
                string departureCode = departureLocations.First().Code;
                _logger.LogInformation("[{Time}] Departure airport code resolved: {Code}", DateTime.UtcNow, departureCode);

                List<Location>? arrivalLocations = await _parserService.GetAirportCodesAsync(subscription.ArrivalAirport);
                _logger.LogDebug("[{Time}] Arrival airport locations: {Locations}", DateTime.UtcNow, arrivalLocations);
                if (arrivalLocations == null || arrivalLocations.Count == 0)
                {
                    _logger.LogWarning("[{Time}] Arrival airport not found: {ArrivalAirport}", DateTime.UtcNow, subscription.ArrivalAirport);
                    continue;
                }
                string arrivalCode = arrivalLocations.First().Code;
                _logger.LogInformation("[{Time}] Arrival airport code resolved: {Code}", DateTime.UtcNow, arrivalCode);

                Guid searchId = await _parserService.GetSearchIdAsync(departureCode, subscription.DepartureDate, arrivalCode, 1);
                _logger.LogInformation("[{Time}] SearchId received: {SearchId}", DateTime.UtcNow, searchId);

                List<FlightTicket> flightTickets = await _parserService.GetTicketsAsync(searchId);
                _logger.LogInformation("[{Time}] Tickets received: {Count}", DateTime.UtcNow, flightTickets.Count);

                List<FlightTicket> filteredTickets = flightTickets
                    .Where(ticket => ticket.Price <= subscription.MaxPrice &&
                        ticket.StopsCount <= subscription.MaxTransfersCount &&
                        ticket.BaggageAmount >= subscription.MinBaggageAmount &&
                        ticket.HandbagsAmount >= subscription.MinHandbagsAmount)
                    .ToList();
                _logger.LogInformation("[{Time}] Tickets after filtering: {Count} (MaxPrice: {MaxPrice}, MaxTransfers: {MaxTransfersCount}, MinBaggage: {MinBaggageAmount}, MinHandbags: {MinHandbagsAmount})", DateTime.UtcNow, filteredTickets.Count, subscription.MaxPrice, subscription.MaxTransfersCount, subscription.MinBaggageAmount, subscription.MinHandbagsAmount);

                string patchUrl = $"http://{_apiUrl}:5148/api/v1/subscriptions/";
                string json = JsonSerializer.Serialize(new { Id = subscription.Id });
                StringContent content = new(json, Encoding.UTF8, "application/json");
                try
                {
                    var patchResponse = await _requestRetryHandler.ExecuteWithRetryAsync(() => _httpClient.PatchAsync(patchUrl, content));
                    if (patchResponse.IsSuccessStatusCode) _logger.LogInformation("[{Time}] PATCH {PatchUrl} succeeded for subscription {Id}", DateTime.UtcNow, patchUrl, subscription.Id);
                    else _logger.LogWarning("[{Time}] PATCH {PatchUrl} failed for subscription {Id} | Status: {StatusCode}", DateTime.UtcNow, patchUrl, subscription.Id, patchResponse.StatusCode);
                }
                catch (Exception patchEx)
                {
                    _logger.LogError(patchEx, "[{Time}] PATCH {PatchUrl} error for subscription {Id}", DateTime.UtcNow, patchUrl, subscription.Id);
                }

                if (filteredTickets.Count == 0)
                {
                    _logger.LogInformation("[{Time}] No suitable tickets found for subscription {Id}", DateTime.UtcNow, subscription.Id);
                }
                else
                {
                    foreach (FlightTicket flightTicket in filteredTickets)
                    {
                        await _queueListenerService.PublishFlightTicketAsync(flightTicket, subscription.ChatId);
                        _logger.LogDebug("[{Time}] Published ticket {TicketId} to queue for subscription {Id}", DateTime.UtcNow, flightTicket.Url, subscription.Id);
                    }
                    _logger.LogInformation("[{Time}] Published {Count} tickets to queue for subscription {Id}", DateTime.UtcNow, filteredTickets.Count, subscription.Id);
                }

                DateTime subEnd = DateTime.UtcNow;
                _logger.LogInformation("[{Time}] [END SUBSCRIPTION] Finished processing subscription {Id}. Duration: {Duration} сек.", subEnd, subscription.Id, (subEnd - subStart).TotalSeconds);
            }
            DateTime processEnd = DateTime.UtcNow;
            _logger.LogInformation("[{Time}] [END] Finished processing all cached subscriptions. Duration: {Duration} сек.", processEnd, (processEnd - processStart).TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Time}] Error processing cached subscriptions", DateTime.UtcNow);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Time}] SubscriptionProcessorService stopping.", DateTime.UtcNow);
        if (_cts != null)
        {
            await _cts.CancelAsync();
            if (_processingTask != null) await _processingTask;
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}