using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketsToSky.TelegramBot;
using TicketsToSky.TelegramBot.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient<IAirportService, AirportService>();
        services.AddHttpClient<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<IUserStateService, UserStateService>();
        services.AddSingleton<IBotService, BotService>();
        services.AddHostedService<BotWorker>();
    })
    .Build();

await host.RunAsync();