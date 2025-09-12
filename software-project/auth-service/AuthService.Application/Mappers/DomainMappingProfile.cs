using AuthService.Application.Features.Permissions.DTOs;
using AuthService.Application.Features.Roles.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.DTOs.Authentication;
using AutoMapper;

namespace AuthService.Application.Mappers;

public class DomainMappingProfile : Profile
{
    public DomainMappingProfile()
    {
        // Permission mappings
        CreateMap<CreatePermissionRequestDto, Permission>()
            .ConstructUsing(src => new Permission(src.Code, src.Name, src.Description));

        CreateMap<Permission, PermissionResponseDto>();

        CreateMap<UpdatePermissionRequestDto, Permission>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .AfterMap((src, dest) => dest.UpdateDetails(src.Name, src.Description));

        // Role mappings
        CreateMap<CreateRoleRequestDto, Role>()
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()) 
            .ConstructUsing(src => new Role(src.Code, src.Name, new List<string>()));

        CreateMap<Role, RoleResponseDto>()
            .ForMember(dest => dest.PermissionCodes, opt => opt.Ignore());

        CreateMap<UpdateRoleRequestDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .AfterMap((src, dest) => dest.UpdateName(src.Name));

        // Login request to User lookup
        CreateMap<LoginRequestDto, Email>()
            .ConstructUsing(src => new Email(src.Email));
    }
}