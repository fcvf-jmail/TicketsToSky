namespace TicketsToSky.Api.Data;

using Microsoft.EntityFrameworkCore;
using TicketsToSky.Api.Configuration;
using TicketsToSky.Api.Models;

public class SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    }
}