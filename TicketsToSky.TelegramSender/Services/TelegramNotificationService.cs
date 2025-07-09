using TicketsToSky.TelegramSender.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TicketsToSky.TelegramSender.Services;

public class TelegramNotificationService : IHostedService, IDisposable
{
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConnection _rabbitConnection;
    private readonly IChannel _rabbitChannel;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;

    public TelegramNotificationService(ILogger<TelegramNotificationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _queueName = configuration["RabbitMQ:TelegramQueueName"] ?? throw new ArgumentNullException("RabbitMQ:TelegramQueueName");
        _exchangeName = configuration["RabbitMQ:TelegramExchangeName"] ?? throw new ArgumentNullException("RabbitMQ:TelegramExchangeName");
        _routingKey = configuration["RabbitMQ:TelegramRoutingKey"] ?? "telegram.message";

        var botToken = configuration["Telegram:BotToken"] ?? throw new ArgumentNullException("Telegram:BotToken");
        _botClient = new TelegramBotClient(botToken);

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? throw new ArgumentNullException("RabbitMQ:HostName"),
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? throw new ArgumentNullException("RabbitMQ:VirtualHost"),
            UserName = configuration["RabbitMQ:UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName"),
            Password = configuration["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password")
        };

        _rabbitConnection = factory.CreateConnectionAsync().Result;
        _rabbitChannel = _rabbitConnection.CreateChannelAsync().Result;

        _rabbitChannel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        ).Wait();

        _rabbitChannel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).Wait();

        _rabbitChannel.QueueBindAsync(_queueName, _exchangeName, _routingKey).Wait();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelegramNotificationService starting");

        var consumer = new AsyncEventingBasicConsumer(_rabbitChannel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var jsonMessage = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<TelegramQueueMessage>(jsonMessage);
                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize TelegramQueueMessage from queue {QueueName}", _queueName);
                    return;
                }

                await _botClient.SendMessage(message.ChatId, message.Message, ParseMode.Html, cancellationToken: cancellationToken, linkPreviewOptions: new LinkPreviewOptions().IsDisabled = true);

                _logger.LogInformation("Sent message to Telegram chat {ChatId}", message.ChatId);

                await _rabbitChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", _queueName);
                await _rabbitChannel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _rabbitChannel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Started consuming messages from queue {QueueName}", _queueName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelegramNotificationService stopping.");
        if (_rabbitChannel != null) await _rabbitChannel.CloseAsync(cancellationToken);
        if (_rabbitConnection != null) await _rabbitConnection.CloseAsync(cancellationToken);
    }

    public void Dispose()
    {
        _rabbitChannel?.Dispose();
        _rabbitConnection?.Dispose();
    }
}