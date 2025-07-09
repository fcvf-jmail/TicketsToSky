using Microsoft.Extensions.Hosting;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public interface IRabbitMQConsumer : IHostedService
{
    public new Task StartAsync(CancellationToken cancellationToken);
    public new Task StopAsync(CancellationToken cancellationToken);
}