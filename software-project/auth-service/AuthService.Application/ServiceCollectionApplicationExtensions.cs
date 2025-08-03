using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { },
             typeof(UserMappingProfile),
             typeof(RefreshMappingProfile));

        return services;
    }
}
