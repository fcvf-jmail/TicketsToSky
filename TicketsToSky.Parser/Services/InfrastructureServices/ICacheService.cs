using TicketsToSky.Parser.Models.SubscriptionModels;

namespace TicketsToSky.Parser.Services.InfrastructureServices;

public interface ICacheService
{
    public Task<T?> GetAsync<T>(string key);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    public Task RemoveAsync(string key);
    public Task<IEnumerable<string>> GetKeysAsync(string pattern);
}