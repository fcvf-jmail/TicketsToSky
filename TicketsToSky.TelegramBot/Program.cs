using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketsToSky.TelegramBot.Services;

namespace TicketsToSky.TelegramBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                // var db = scope.ServiceProvider.GetRequiredService<Data.UserStateDbContext>();
                // db.Database.EnsureCreated();
            }
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;
                    services.AddHostedService<BotService>();
                    services.AddSingleton<ITelegramBotService, TelegramBotService>();
                    services.AddSingleton<IAirportService, AirportService>();
                    services.AddSingleton<ISubscriptionService, SubscriptionService>();
                    services.AddScoped<UserStateStorageService>();
                    services.AddScoped<SubscriptionStorageService>();
                    services.AddHttpClient();
                    services.AddUserStateDb(config);
                });
    }
}