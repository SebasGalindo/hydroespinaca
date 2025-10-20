using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Users.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));
    }
}