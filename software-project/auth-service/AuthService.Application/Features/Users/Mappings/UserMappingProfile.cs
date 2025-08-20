using AuthService.Application.Features.Users.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Features.Users.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));
    }
}