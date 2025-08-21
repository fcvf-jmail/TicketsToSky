using Microsoft.EntityFrameworkCore;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Data
{
    public class UserStateDbContext(DbContextOptions<UserStateDbContext> options) : DbContext(options)
    {

        public DbSet<UserStateEntity> UserStates { get; set; }
        public DbSet<SubscriptionEntity> Subscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStateEntity>().HasKey(x => x.ChatId);
            modelBuilder.Entity<SubscriptionEntity>().HasKey(x => x.Id);
        }
    }
}
