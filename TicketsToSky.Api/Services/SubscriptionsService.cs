namespace TicketsToSky.Api.Services;

using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketsToSky.Api.Models;
using TicketsToSky.Api.Repositories;

public class SubscriptionsService(ISubscriptionRepository subscriptionRepository, IMapper mapper, IRabbitMQService rabbitMQService) : ISubscriptionsService
{
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly IRabbitMQService _rabbitMQService = rabbitMQService;
    private readonly IMapper _mapper = mapper;

    public async Task<Guid> CreateAsync(SubscriptionDto subscriptionDto)
    {
        if (subscriptionDto.Id == Guid.Empty) subscriptionDto.Id = Guid.NewGuid();
        Subscription subscription = _mapper.Map<Subscription>(subscriptionDto);

        subscription.CreatedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.LastChecked = null;

        SubscriptionEvent subscriptionEvent = _mapper.Map<SubscriptionEvent>(subscriptionDto);
        subscriptionEvent.Event = RabbitMqEventEnum.Created;

        Guid subscriptionId = await _subscriptionRepository.AddAsync(subscription);
        await _rabbitMQService.PublishEventAsync(subscriptionEvent, "subscription.created");

        return subscriptionId;
    }

    public async Task DeleteAsync(Guid id)
    {
        Subscription subscription = await _subscriptionRepository.GetByIdAsync(id);
        SubscriptionEvent subscriptionEvent = _mapper.Map<SubscriptionEvent>(subscription);
        subscriptionEvent.Event = RabbitMqEventEnum.Deleted;

        await _subscriptionRepository.DeleteAsync(id);
        await _rabbitMQService.PublishEventAsync(subscriptionEvent, "subscription.deleted");
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionsAsync()
    {
        List<Subscription> subscriptions = await _subscriptionRepository.GetSubscriptionsAsync();
        List<SubscriptionDto> subscriptionDtos = [];

        foreach (Subscription subscription in subscriptions)
        {
            SubscriptionDto subscriptionDto = _mapper.Map<SubscriptionDto>(subscription);
            subscriptionDtos.Add(subscriptionDto);
        }
        return subscriptionDtos;
    }

    public async Task<Guid> UpdateAsync(SubscriptionDto subscriptionDto)
    {
        Subscription subscription = await _subscriptionRepository.GetByIdAsync(subscriptionDto.Id);
        _mapper.Map(subscriptionDto, subscription);

        subscription.CreatedAt = subscription.CreatedAt;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.LastChecked = subscription.LastChecked;

        SubscriptionEvent subscriptionEvent = _mapper.Map<SubscriptionEvent>(subscription);
        subscriptionEvent.Event = RabbitMqEventEnum.Updated;

        Guid subscriptionId = await _subscriptionRepository.UpdateAsync(subscription);
        await _rabbitMQService.PublishEventAsync(subscriptionEvent, "subscription.updated");

        return subscriptionId;
    }

    public async Task<Guid> UpdateLastCheckedAsync(Guid id, DateTime updatedTime)
    {
        return await _subscriptionRepository.UpdateLastCheckedAsync(id, updatedTime);
    }
}