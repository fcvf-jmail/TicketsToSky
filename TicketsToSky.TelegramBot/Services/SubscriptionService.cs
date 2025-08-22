using System.Net.Http.Json;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;

    public SubscriptionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateSubscriptionAsync(Subscription subscription)
    {
        await _httpClient.PostAsJsonAsync("http://45.95.174.246:5148/api/v1/subscriptions", subscription);
    }
}