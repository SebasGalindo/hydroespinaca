using AutoMapper;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.NotificationGroups.Mappings;

/// <summary>
/// Defines the AutoMapper profile for mapping between domain entities and DTOs related 
/// to notification groups.
/// </summary>
public class NotificationGroupMappingProfile : Profile
{
    public NotificationGroupMappingProfile()
    {
        // Map between NotificationGroup domain entity and NotificationGroupDto
        CreateMap<NotificationGroup, NotificationGroupDto>();

        // Map between NotificationGroupDto and NotificationGroup domain entity
        CreateMap<GroupRecipient, GroupRecipientDto>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => Enum.Parse<RecipientTypeDto>(src.Type.ToString())));
                
        // Map between GroupRecipientDto and GroupRecipient domain entity
        CreateMap<GroupRecipientDto, GroupRecipient>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => Enum.Parse<RecipientType>(src.Type.ToString())));
    }
}
