using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Permissions.Mappings;

public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        CreateMap<Permission, PermissionResponseDto>();
    }
}