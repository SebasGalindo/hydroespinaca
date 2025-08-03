using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Mappers;

public class RefreshMappingProfile : Profile
{
    public RefreshMappingProfile()
    {
        CreateMap<RefreshToken, TokenResponseDto>()
            .ForMember(dest => dest.RefreshToken, opt => opt.MapFrom(src => src.Token))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))

            .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.ClientId, opt => opt.Ignore());

        CreateMap<RefreshRequestDto, RefreshToken>()
            .ConstructUsing(dto =>
                new RefreshToken(
                    Guid.Empty,
                    dto.RefreshToken,
                    DateTime.UtcNow,
                    dto.ClientId ?? string.Empty
                )
            );
    }
}
