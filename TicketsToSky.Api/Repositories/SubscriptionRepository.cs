
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using TicketsToSky.Api.Data;
using Microsoft.EntityFrameworkCore;
using TicketsToSky.Api.Models;

namespace TicketsToSky.Api.Repositories;

public class SubscriptionRepository(SubscriptionsDbContext dbContext) : ISubscriptionRepository
{
    private readonly SubscriptionsDbContext _context = dbContext;

    public async Task<Guid> AddAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
        return subscription.Id;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.Subscriptions.Where(x => x.Id == id).ExecuteDeleteAsync();
        await _context.SaveChangesAsync();
        return;
    }

    public async Task<Subscription> GetByIdAsync(Guid id)
    {
        return await _context.Subscriptions.FirstAsync(x => x.Id == id);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        return await _context.Subscriptions.ToListAsync();
    }

    public async Task<Guid> UpdateAsync(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();

        return subscription.Id;
    }

    public async Task<Guid> UpdateLastCheckedAsync(Guid id, DateTime updatedTime)
    {
        Subscription subscription = await _context.Subscriptions.FirstAsync(x => x.Id == id);
        subscription.LastChecked = updatedTime;
        await _context.SaveChangesAsync();
        return subscription.Id;
    }
}
