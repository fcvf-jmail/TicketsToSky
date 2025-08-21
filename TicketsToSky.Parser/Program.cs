using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using TicketsToSky.Parser.Services.IntegrationServices;
using TicketsToSky.Parser.Services.InfrastructureServices;
using TicketsToSky.Parser.Services.BusinessServices;
using Microsoft.Playwright;

List<string> directories = [.. Directory.GetDirectories(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH"))];
string? chromiumDirectory = directories.Find(directory => directory.Contains("chromium-"));
string? chromeDirectory = Directory.GetDirectories(chromiumDirectory).ToList().Find(directory => directory.Contains("chrome"));

IHostBuilder builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton(sp => Playwright.CreateAsync().GetAwaiter().GetResult());

    services.AddSingleton(sp =>
    {
        IPlaywright playwright = sp.GetRequiredService<IPlaywright>();
        return playwright.Chromium.LaunchAsync(new() { Headless = false, ExecutablePath = Path.Join(chromeDirectory, "chrome") }).GetAwaiter().GetResult();
    });

    services.AddHttpClient<IApiClient, ApiClient>();
    services.AddSingleton<IPlaywrightService, PlaywrightService>();
    services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(hostContext.Configuration["Redis:ConnectionString"]));
    services.AddSingleton<ICacheService, RedisCacheService>();
    services.AddSingleton<IRabbitMQConsumer, SearchParamsConsumer>();
    services.AddSingleton<IJsonParser, JsonParser>();
    services.AddSingleton<ITicketConverter, TicketConverter>();
    services.AddSingleton<IParserService, ParserService>();
    services.AddSingleton<IRequestRetryHandler, RequestRetryHandler>();
    services.AddHostedService<SearchParamsConsumer>();
    services.AddSingleton<QueueListenerService>();
    services.AddHostedService<QueueListenerService>();
    services.AddHostedService<SubscriptionProcessorService>();
});

var host = builder.Build();
await host.RunAsync();