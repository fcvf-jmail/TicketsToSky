using System.Net;
using Microsoft.Extensions.Logging;
namespace TicketsToSky.Parser.Services.InfrastructureServices;

public class RequestRetryHandler : IRequestRetryHandler
{
    public async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> request, int maxRetries = 3, int delayMs = 1000)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                HttpResponseMessage response = await request();
                if (response.IsSuccessStatusCode) return response;

                if (attempt >= maxRetries || (response.StatusCode != HttpStatusCode.TooManyRequests && response.StatusCode != HttpStatusCode.ServiceUnavailable))
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }

                Console.WriteLine($"Attempt {attempt} failed with status {response.StatusCode}. Retrying after {delayMs}ms...");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries && (ex.StatusCode == HttpStatusCode.TooManyRequests || ex.StatusCode == HttpStatusCode.ServiceUnavailable))
            {
                Console.WriteLine($"Attempt {attempt} failed with status {ex.StatusCode}. Retrying after {delayMs}ms...");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (TaskCanceledException) when (attempt < maxRetries)
            {
                Console.WriteLine($"Attempt {attempt} timed out. Retrying after {delayMs}ms...");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (Exception ex) when (attempt >= maxRetries)
            {
                Console.WriteLine($"All {maxRetries} attempts failed. Last error: {ex.Message}");
                throw;
            }
        }
    }
}