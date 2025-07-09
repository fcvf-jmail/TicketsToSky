namespace TicketsToSky.Api.Repositories;

using TicketsToSky.Api.Models;

public interface ISubscriptionRepository
{
    Task<Guid> AddAsync(Subscription subscription);
    Task<Guid> UpdateAsync(Subscription subscription);
    Task DeleteAsync(Guid id);
    Task<List<Subscription>> GetSubscriptionsAsync();
    Task<Guid> UpdateLastCheckedAsync(Guid id, DateTime updatedTime);
    Task<Subscription> GetByIdAsync(Guid id);
}