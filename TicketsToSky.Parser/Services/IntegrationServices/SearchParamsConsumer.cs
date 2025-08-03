namespace TicketsToSky.Parser.Services.IntegrationServices;

using System.Text;
using System.Text.Json;
using TicketsToSky.Parser.Models.SubscriptionModels;
using TicketsToSky.Parser.Services.BusinessServices;
using TicketsToSky.Parser.Services.InfrastructureServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class SearchParamsConsumer : IRabbitMQConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<SearchParamsConsumer> _logger;
    private readonly IParserService _parserService;
    private readonly ICacheService _cacheService;

    private readonly string _queueName;
    private readonly string _exchangeName;

    public SearchParamsConsumer(IConfiguration configuration, ILogger<SearchParamsConsumer> logger, IParserService parserService, ICacheService cacheService)
    {
        ConnectionFactory factory = new()
        {
            HostName = configuration["RabbitMQ:HostName"] ?? throw new ArgumentNullException("RabbitMQ:HostName"),
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? throw new ArgumentNullException("RabbitMQ:VirtualHost"),
            UserName = configuration["RabbitMQ:UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName"),
            Password = configuration["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password")
        };
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
        _logger = logger;
        _parserService = parserService;
        _cacheService = cacheService;

        _queueName = configuration["RabbitMQ:QueueName"] ?? throw new ArgumentNullException("RabbitMQ:QueueName", "Queue name must be configured.");
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? throw new ArgumentNullException("RabbitMQ:ExchangeName", "Exchange name must be configured.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _queueName,
            exchange: _exchangeName,
            "subscription.*",
            cancellationToken: cancellationToken
        );

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += HandleReceivedMessageAsync;


        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _channel?.CloseAsync();
        await _connection?.CloseAsync();
    }

    private async Task HandleReceivedMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            SubscriptionEvent? subscriptionEvent = GetSubscriptionEvent(ea);
            _logger.LogInformation($"Received event: {subscriptionEvent?.Event} for subscription {subscriptionEvent?.Id}");

            switch (subscriptionEvent?.Event)
            {
                case RabbitMqEventEnum.Created:
                    await ProcessCreatedEventAsync(subscriptionEvent);
                    break;

                case RabbitMqEventEnum.Updated:
                    await ProcessUpdatedEventAsync(subscriptionEvent);
                    break;

                case RabbitMqEventEnum.Deleted:
                    await ProcessDeletedEventAsync(subscriptionEvent);
                    break;

                default:
                    _logger.LogWarning($"Unknown event type: {subscriptionEvent?.Event} for subscription {subscriptionEvent?.Id}");
                    break;
            }
            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing message: {ex.Message}");
            await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }
    private async Task ProcessCreatedEventAsync(SubscriptionEvent subscriptionEvent)
    {
        _logger.LogInformation($"Processing Created event for subscription {subscriptionEvent.Id}");
        await _cacheService.SetAsync($"Subscription:{subscriptionEvent.Id}", subscriptionEvent);
        _logger.LogInformation($"Cached SubscriptionEvent for subscription {subscriptionEvent.Id}");
    }

    private async Task ProcessUpdatedEventAsync(SubscriptionEvent subscriptionEvent)
    {
        _logger.LogInformation($"Processing Updated event for subscription {subscriptionEvent.Id}");
        await _cacheService.SetAsync($"Subscription:{subscriptionEvent.Id}", subscriptionEvent, null);
        _logger.LogInformation($"Updated SubscriptionEvent for subscription {subscriptionEvent.Id} in cache");
    }

    private async Task ProcessDeletedEventAsync(SubscriptionEvent subscriptionEvent)
    {
        _logger.LogInformation($"Processing Deleted event for subscription {subscriptionEvent.Id}");
        await _cacheService.RemoveAsync($"Subscription:{subscriptionEvent.Id}");
        _logger.LogInformation($"Removed tickets and SubscriptionEvent for subscription {subscriptionEvent.Id} from cache");
    }

    private SubscriptionEvent? GetSubscriptionEvent(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            return JsonSerializer.Deserialize<SubscriptionEvent>(message);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Failed to deserialize message: {ex.Message}");
            return null;
        }
    }

}