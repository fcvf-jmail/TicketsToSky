using AutoMapper;
using TicketsToSky.Api.Models;

namespace TicketsToSky.Api.Mappings
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionDto, Subscription>().ForMember(dest => dest.CreatedAt, opt => opt.Ignore()).ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()).ForMember(dest => dest.LastChecked, opt => opt.Ignore());

            CreateMap<Subscription, SubscriptionDto>();

            CreateMap<Subscription, SubscriptionEvent>().ForMember(dest => dest.Event, opt => opt.Ignore());
            CreateMap<SubscriptionDto, SubscriptionEvent>().ForMember(dest => dest.Event, opt => opt.Ignore());
        }
    }

}