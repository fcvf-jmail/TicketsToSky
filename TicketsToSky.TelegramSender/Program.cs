using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketsToSky.TelegramSender.Services;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<TelegramNotificationService>();
    services.AddSingleton(hostContext.Configuration);
});

var host = builder.Build();
await host.RunAsync();