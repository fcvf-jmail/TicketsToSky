namespace TicketsToSky.Api.Services;

using TicketsToSky.Api.Models;

public interface ISubscriptionsService
{
    Task<Guid> CreateAsync(SubscriptionDto subscriptionDto);
    Task<Guid> UpdateAsync(SubscriptionDto subscriptionDto);
    Task DeleteAsync(Guid id);
    Task<List<SubscriptionDto>> GetSubscriptionsAsync();
    Task<Guid> UpdateLastCheckedAsync(Guid id, DateTime updatedTime);
}