using TicketsToSky.Api.Models;

namespace TicketsToSky.Api.Services;

public interface IRabbitMQService
{
    public Task PublishEventAsync(SubscriptionEvent subscriptionEvent, string routingKey);
}