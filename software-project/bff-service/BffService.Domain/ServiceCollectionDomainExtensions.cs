using Microsoft.Extensions.DependencyInjection;
using BffService.Domain.Services;

namespace BffService.Domain;

public static class ServiceCollectionDomainExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<SessionValidationService>();
        
        return services;
    }
}