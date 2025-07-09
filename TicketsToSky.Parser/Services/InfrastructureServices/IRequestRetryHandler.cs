namespace TicketsToSky.Parser.Services.InfrastructureServices;

public interface IRequestRetryHandler
{
    public Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> request, int maxRetries = 3, int delayMs = 1000);
}