using Microsoft.Extensions.DependencyInjection;
using BffService.Domain.Services;

namespace BffService.Domain;

/// <summary>
/// Extension methods for registering BFF domain layer services.
/// </summary>
public static class ServiceCollectionDomainExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<SessionValidationService>();
        
        return services;
    }
}