namespace TicketsToSky.TelegramSender.Models;

public class TelegramQueueMessage
{
    public long ChatId { get; set; }
    public required string Message { get; set; }
}