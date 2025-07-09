namespace TicketsToSky.Parser.Models.OtherModels;

public class TelegramQueueMessage
{
    public long ChatId { get; set; }
    public required string Message { get; set; }
}