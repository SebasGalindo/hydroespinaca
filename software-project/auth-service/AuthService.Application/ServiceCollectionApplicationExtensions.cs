using AuthService.Application.Mappers;
using AuthService.Application.UseCases;
using AuthService.Application.Validators;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { },
             typeof(UserMappingProfile),
             typeof(RefreshMappingProfile));

        services.AddScoped<AuthenticateUserUseCase>();
        services.AddScoped<ClientCredentialsUseCase>();
        services.AddScoped<CreateClientAppUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<ValidateTokenUseCase>();

      

        return services;
    }
}
