using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using TicketsToSky.Parser.Services.IntegrationServices;
using TicketsToSky.Parser.Services.InfrastructureServices;
using TicketsToSky.Parser.Services.BusinessServices;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddHttpClient<IApiClient, ApiClient>();
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