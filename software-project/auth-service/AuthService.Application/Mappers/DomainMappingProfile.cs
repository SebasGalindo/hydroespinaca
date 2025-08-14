using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
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

        // Role mappings - will need custom logic to resolve permission codes to ObjectIds
        CreateMap<CreateRoleRequestDto, Role>()
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()) // Will be handled in use case
            .ConstructUsing(src => new Role(src.Code, src.Name, new List<string>()));

        CreateMap<Role, RoleResponseDto>()
            .ForMember(dest => dest.PermissionCodes, opt => opt.Ignore()); // Will be mapped in use case

        CreateMap<UpdateRoleRequestDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore()) // Will be handled in use case
            .AfterMap((src, dest) => dest.UpdateName(src.Name));

        // User mappings
        CreateMap<User, TokenResponseDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.RoleId ?? ""))
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.ClientId, opt => opt.Ignore());

        // Login request to User lookup
        CreateMap<LoginRequestDto, Email>()
            .ConstructUsing(src => new Email(src.Email));
    }
}