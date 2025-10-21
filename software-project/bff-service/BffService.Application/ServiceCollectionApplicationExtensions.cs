using Microsoft.Extensions.DependencyInjection;
using BffService.Application.Interfaces;
using BffService.Application.Services;
using BffService.Domain.Interfaces;

namespace BffService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache(); // For caching analytics responses

        services.AddScoped<ISessionService, SessionApplicationService>();
        services.AddScoped<ICsrfValidationService, CsrfValidationService>();
        services.AddScoped<ISystemStatusService, SystemStatusService>();
        services.AddScoped<ISessionTokenService, SessionTokenService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}