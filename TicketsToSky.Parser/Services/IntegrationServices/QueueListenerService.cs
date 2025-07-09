using TicketsToSky.Parser.Models.FlightModels;
using TicketsToSky.Parser.Models.OtherModels;
using TicketsToSky.Parser.Models.SearchModels;
using TicketsToSky.Parser.Services.InfrastructureServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class QueueListenerService : IHostedService, IDisposable
{
    private readonly ILogger<QueueListenerService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IConnection _rabbitConnection;
    private readonly IChannel _rabbitChannel;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;

    public QueueListenerService(
        ILogger<QueueListenerService> logger,
        ICacheService cacheService,
        IConfiguration configuration)
    {
        _logger = logger;
        _cacheService = cacheService;

        _queueName = configuration["RabbitMQ:TelegramQueueName"] ?? throw new ArgumentNullException("RabbitMQ:TelegramQueueName");
        _exchangeName = configuration["RabbitMQ:TelegramExchangeName"] ?? throw new ArgumentNullException("RabbitMQ:TelegramExchangeName");
        _exchangeName = configuration["RabbitMQ:UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName");
        _routingKey = configuration["RabbitMQ:TelegramRoutingKey"] ?? "telegram.message";

        ConnectionFactory factory = new()
        {
            HostName = configuration["RabbitMQ:HostName"] ?? throw new ArgumentNullException("RabbitMQ:HostName"),
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? throw new ArgumentNullException("RabbitMQ:VirtualHost"),
            UserName = configuration["RabbitMQ:UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName"),
            Password = configuration["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password")
        };

        _rabbitConnection = factory.CreateConnectionAsync().Result;
        _rabbitChannel = _rabbitConnection.CreateChannelAsync().Result;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueListenerService starting.");

        await _rabbitChannel.ExchangeDeclareAsync(
            exchange: _exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _rabbitChannel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _rabbitChannel.QueueBindAsync(_queueName, _exchangeName, _routingKey, cancellationToken: cancellationToken);

        _logger.LogInformation("QueueListenerService configured RabbitMQ exchange {ExchangeName} and queue {QueueName}.", _exchangeName, _queueName);
    }

    public async Task PublishFlightTicketAsync(FlightTicket ticket, long chatId)
    {
        try
        {
            string ticketKey = CreateFlightTicketMessageKey(ticket);
            string? cachedTicket = await _cacheService.GetAsync<string>(ticketKey);

            if (cachedTicket != null)
            {
                _logger.LogInformation("Ticket {TicketKey} already sent, skipping", ticketKey);
                return;
            }

            string message = GetMessageText(ticket);
            string jsonString = JsonSerializer.Serialize(new TelegramQueueMessage
            {
                ChatId = chatId,
                Message = message
            });

            byte[] body = Encoding.UTF8.GetBytes(jsonString);

            await _rabbitChannel.BasicPublishAsync(_exchangeName, _routingKey, body);
            await _cacheService.SetAsync(ticketKey, "sent");
            _logger.LogInformation("Published ticket message for chat {ChatId} to queue {QueueName}", chatId, _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing ticket message for chat {ChatId}", chatId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueListenerService stopping.");
        if (_rabbitChannel != null) await _rabbitChannel.CloseAsync(cancellationToken);
        if (_rabbitConnection != null) await _rabbitConnection.CloseAsync(cancellationToken);
    }

    public void Dispose()
    {
        _rabbitChannel?.Dispose();
        _rabbitConnection?.Dispose();
    }

    public static string GetMessageText(FlightTicket ticket)
    {
        StringBuilder message = new();
        message.AppendLine("🎉 Найден авиабилет!");
        message.AppendLine($"✈️ Рейс: {ticket.Flights.First().DepartureCityName} ({ticket.Flights.First().DepartureAirportName}) - {ticket.Flights.Last().ArrivalCityName} ({ticket.Flights.Last().ArrivalAirportName})");
        message.AppendLine($"🗓 {ticket.DepartureDateTime:dd.MM.yyyy HH:mm} – {ticket.ArrivalDateTime:dd.MM.yyyy HH:mm}");
        message.AppendLine($"🕒 Длительность: {ticket.TotalDuration / 60}ч {ticket.TotalDuration % 60}мин");
        message.AppendLine($"🛬 Пересадки: {(ticket.StopsCount > 0 ? $"{ticket.StopsCount}" : "Прямой рейс")}");

        string baggageInfo = ticket.BaggageAmount == 0 ? "без багажа" : $"{ticket.BaggageAmount} {(ticket.BaggageInfo.Length > 3 ? $"({ticket.BaggageInfo})" : "")}";
        string handbagsInfo = ticket.HandbagsAmount == 0 ? "без ручной клади" : $"{ticket.HandbagsAmount} {(ticket.HandbagsInfo.Length > 3 ? $"({ticket.HandbagsInfo})" : "")}";
        int amountOfPassengers = 1;
        message.AppendLine($"\n💸 Цена: {ticket.Price} {ticket.Currency}");
        message.AppendLine($"🧳 Багаж: {baggageInfo}");
        message.AppendLine($"🎒 Ручная кладь: {handbagsInfo}");

        if (ticket.StopsCount > 0)
        {
            message.AppendLine();
            for (int i = 0; i < ticket.Flights.Count; i++)
            {
                var flight = ticket.Flights[i];
                message.AppendLine($"✈️ Перелет {i + 1}: {flight.DepartureCityName} ({flight.DepartureAirportName}) {flight.DepartureDateTime:dd.MM.yyyy HH:mm} - {flight.ArrivalDateTime:dd.MM.yyyy HH:mm} {flight.ArrivalCityName} ({flight.ArrivalAirportName})");
                if (i < ticket.Transfers.Count)
                {
                    Transfer transfer = ticket.Transfers[i];
                    string transferInfo = string.Empty;
                    if (transfer.FromAirport != transfer.ToAirport) transferInfo = $"‼️ Пересадка со сменой аэропорта {transfer.FromAirportName} ({transfer.FromCityName}) - {transfer.ToAirportName} ({transfer.ToCityName}): ";
                    else transferInfo = $"⏳ Пересадка в {transfer.FromAirportName} ({transfer.FromCityName}): ";
                    transferInfo += $"{transfer.DurationMinutes / 60}ч {transfer.DurationMinutes % 60}мин (до {transfer.EndDateTime:dd.MM.yyyy HH:mm})";

                    message.AppendLine(transferInfo);
                }
            }
        }

        string loukosterLink = $"https://avia.loukoster.com/flights/{ticket.DepartureAirport}{ticket.DepartureDateTime.Day.ToString().PadLeft(2, '0')}{ticket.DepartureDateTime.Month.ToString().PadLeft(2, '0')}{ticket.ArrivalAirport}{amountOfPassengers}";
        message.AppendLine($"\n🔗 <a href=\"{loukosterLink}\">Другие билеты</a>");
        message.AppendLine($"💰 <a href=\"{ticket.LinkToBuy}\">Купить билет</a>");

        return message.ToString();
    }

    public static string CreateFlightTicketMessageKey(FlightTicket ticket)
    {
        var sb = new StringBuilder();

        sb.Append($"Tickets:{ticket.Flights.First().DepartureCityName}:{ticket.Flights.First().DepartureAirportName}:");
        sb.Append($"{ticket.Flights.Last().ArrivalCityName}:{ticket.Flights.Last().ArrivalAirportName}:");
        sb.Append($"{ticket.DepartureDateTime:yyyyMMddHHmm}:{ticket.ArrivalDateTime:yyyyMMddHHmm}:");
        sb.Append($"{ticket.TotalDuration}:{ticket.StopsCount}:");
        sb.Append($"{ticket.Price}:{ticket.Currency}:");

        string baggageInfo = ticket.BaggageAmount == 0 ? "0" : $"{ticket.BaggageAmount}:{ticket.BaggageInfo}";
        string handbagsInfo = ticket.HandbagsAmount == 0 ? "0" : $"{ticket.HandbagsAmount}:{ticket.HandbagsInfo}";
        sb.Append($"{baggageInfo}:{handbagsInfo}:");

        for (int i = 0; i < ticket.Flights.Count; i++)
        {
            var flight = ticket.Flights[i];
            sb.Append($"{flight.DepartureCityName}:{flight.DepartureAirportName}:");
            sb.Append($"{flight.ArrivalCityName}:{flight.ArrivalAirportName}:");
            sb.Append($"{flight.DepartureDateTime:yyyyMMddHHmm}:{flight.ArrivalDateTime:yyyyMMddHHmm}:");

            if (i < ticket.Transfers.Count)
            {
                var transfer = ticket.Transfers[i];
                sb.Append($"{transfer.FromAirportName}:{transfer.FromCityName}:");
                sb.Append($"{transfer.ToAirportName}:{transfer.ToCityName}:");
                sb.Append($"{transfer.DurationMinutes}:{transfer.EndDateTime:yyyyMMddHHmm}:");
                sb.Append($"{(transfer.FromAirport != transfer.ToAirport ? "1" : "0")}:");
            }
        }

        return sb.ToString();
    }
}