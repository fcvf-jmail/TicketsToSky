using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TicketsToSky.Parser.Services.IntegrationServices;

public class PlaywrightService(IBrowser browser) : IPlaywrightService
{
    private readonly IBrowser _browser = browser;
    public async Task<(List<string> responseBodies, Guid searchId)> GetSearchResultsAsync(string departureCode, DateOnly departureDate, string destinationCode, int amountOfAdults, int amountOfChildren = 0, int amountOfInfants = 0)
    {
        IPage page = await _browser.NewPageAsync();
        List<string> responseBodies = [];
        Guid searchId = Guid.Empty;

        page.Response += async (_, response) =>
        {
            if (!response.Url.Contains("searches_results_united?uuid=")) return;
            string body = await response.TextAsync();
            string searchIdPattern = @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
            if (searchId == Guid.Empty) searchId = Guid.Parse(Regex.Match(response.Url, searchIdPattern).Value);
            responseBodies.Add(body);
        };

        string url = $"https://avia.loukoster.com/flights/{departureCode}{departureDate.Day.ToString().PadLeft(2, '0')}{departureDate.Month.ToString().PadLeft(2, '0')}{destinationCode}{amountOfAdults}{amountOfChildren}{amountOfInfants}?currency=rub&language=ru&locale=ru";
        Console.WriteLine($"Opening url {url}");
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.CloseAsync();
        
        return (responseBodies, searchId);
    }
}