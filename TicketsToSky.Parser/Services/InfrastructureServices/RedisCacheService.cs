namespace TicketsToSky.Parser.Services.InfrastructureServices;

using System.Text.Json;
using TicketsToSky.Parser.Models.SubscriptionModels;
using StackExchange.Redis;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task<T?> GetAsync<T>(string key)
    {
        IDatabase db = _redis.GetDatabase();
        RedisValue value = await db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }

    public async Task RemoveAsync(string key)
    {
        IDatabase db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
        return;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        IDatabase db = _redis.GetDatabase();
        await db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry);
        return;
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var keys = new List<string>();

        await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 100))
        {
            keys.Add(key.ToString());
        }

        return keys;
    }


}