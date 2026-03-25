using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Roles.Mappings;

/// <summary>
/// AutoMapper profile for mapping between Role domain entities and DTOs.
/// </summary>
public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleResponseDto>()
           .ForMember(dest => dest.PermissionCodes, opt => opt.MapFrom(src => src.Permissions));
    }
}