using Microsoft.Extensions.DependencyInjection;
using BffService.Application.Interfaces;
using BffService.Application.Services;

namespace BffService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISessionService, SessionApplicationService>();
        services.AddScoped<ICsrfValidationService, CsrfValidationService>();
        
        return services;
    }
}