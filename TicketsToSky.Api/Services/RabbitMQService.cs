namespace TicketsToSky.Api.Services;

using System.Text;
using System.Text.Json;
using TicketsToSky.Api.Models;
using RabbitMQ.Client;

public class RabbitMQService : IRabbitMQService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private readonly string _hostName;
    private readonly string _virtualHost;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;

    public RabbitMQService(IConfiguration configuration)
    {
        _hostName = configuration["RabbitMQ:HostName"] ?? throw new ArgumentNullException("RabbitMQ:HostName not defined");
        _virtualHost = configuration["RabbitMQ:VirtualHost"] ?? throw new ArgumentNullException("RabbitMQ:VirtualHost not defined");
        _userName = configuration["RabbitMQ:UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName not defined");
        _password = configuration["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password not defined");
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? throw new ArgumentNullException("RabbitMQ:ExchangeName not defined");
        _queueName = configuration["RabbitMQ:QueueName"] ?? throw new ArgumentNullException("RabbitMQ:QueueName not defined");
        _routingKey = configuration["RabbitMQ:RoutingKey"] ?? throw new ArgumentNullException("RabbitMQ:RoutingKey not defined");


        ConnectionFactory factory = new()
        {
            HostName = _hostName,
            VirtualHost = _virtualHost,
            UserName = _userName,
            Password = _password
        };

        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;

        _channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        ).Wait();

        _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        ).Wait();

        _channel.QueueBindAsync(_queueName, _exchangeName, _routingKey).Wait();
    }
    public async Task PublishEventAsync(SubscriptionEvent subscriptionEvent, string routingKey)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        string message = JsonSerializer.Serialize(subscriptionEvent);
        byte[] body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(_exchangeName, routingKey, body);
    }
}