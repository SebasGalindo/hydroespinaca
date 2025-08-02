using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Mappers;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, TokenResponseDto>()
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.ClientId, opt => opt.Ignore())

            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}
