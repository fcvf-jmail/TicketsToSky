using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly HttpClient _httpClient;

        public SubscriptionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task CreateSubscriptionAsync(Subscription subscription)
        {
            subscription.Id = Guid.NewGuid().ToString();
            var json = JsonSerializer.Serialize(subscription);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:5148/api/v1/subscriptions", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<Subscription> GetSubscriptionAsync(string id)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5148/api/v1/subscriptions/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Subscription>(content) ?? throw new InvalidOperationException("Failed to deserialize subscription");
        }

        public async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            var json = JsonSerializer.Serialize(subscription);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("http://localhost:5148/api/v1/subscriptions", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteSubscriptionAsync(string id)
        {
            var json = JsonSerializer.Serialize(new { id });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("http://localhost:5148/api/v1/subscriptions/"),
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Subscription>> GetUserSubscriptionsAsync(long chatId)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5148/api/v1/subscriptions?chatId={chatId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Subscription>>(content) ?? new List<Subscription>();
        }
    }
}