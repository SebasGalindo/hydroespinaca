using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Permissions.Mappings;

/// <summary>
/// AutoMapper profile for mapping between Permission domain entities and DTOs.
/// </summary>
public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        CreateMap<Permission, PermissionResponseDto>();
    }
}