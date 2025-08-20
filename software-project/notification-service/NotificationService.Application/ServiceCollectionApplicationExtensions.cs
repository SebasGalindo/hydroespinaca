using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.UseCases;
using NotificationService.Application.Interfaces;

namespace NotificationService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
            services.AddScoped<SendEmailUseCase>();
            services.AddScoped<IEmailNotificationService>(sp => sp.GetRequiredService<SendEmailUseCase>());
        return services;
    }
}
