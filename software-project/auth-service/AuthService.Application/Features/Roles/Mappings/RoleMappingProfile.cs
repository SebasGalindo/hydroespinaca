using AuthService.Application.Features.Roles.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Roles.Mappings;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleResponseDto>()
            .ForMember(dest => dest.PermissionCodes, opt => opt.Ignore());
    }
}