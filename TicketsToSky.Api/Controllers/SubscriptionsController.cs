namespace TicketsToSky.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TicketsToSky.Api.Models;
using TicketsToSky.Api.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/api/v1/subscriptions")]

public class SubscriptionsController(ISubscriptionsService subscriptionsService) : ControllerBase
{
    private readonly ISubscriptionsService _subscriptionsService = subscriptionsService;

    [HttpPost]
    public async Task<Guid> CreateSubscription(SubscriptionDto subscriptionDto)
    {
        return await _subscriptionsService.CreateAsync(subscriptionDto);
    }

    [HttpPut]
    public async Task<Guid> UpdateSubscription(SubscriptionDto subscriptionDto)
    {
        return await _subscriptionsService.UpdateAsync(subscriptionDto);
    }

    [HttpDelete]
    public async Task DeleteSubscription(RequestOnlyWithId deleteSubscriptionRequest)
    {
        await _subscriptionsService.DeleteAsync(deleteSubscriptionRequest.Id);
        return;
    }

    [HttpGet]
    public async Task<List<SubscriptionDto>> GetAllSubscriptions()
    {
        return await _subscriptionsService.GetSubscriptionsAsync();
    }

    [HttpGet("/{id}")]
    public async Task<SubscriptionDto> GetSubscription(Guid id)
    {
        return await _subscriptionsService.GetSubscriptionAsync(id);
    }

    [HttpPatch]
    public async Task<Guid> UpdateLastCheckedProperty(RequestOnlyWithId updateLastCheckedRequest)
    {
        Console.WriteLine(updateLastCheckedRequest.Id);
        return await _subscriptionsService.UpdateLastCheckedAsync(updateLastCheckedRequest.Id, DateTime.UtcNow);
    }
}