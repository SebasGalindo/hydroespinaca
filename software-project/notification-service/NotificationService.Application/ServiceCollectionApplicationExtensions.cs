using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Features.NotificationGroups.Mappings;
using NotificationService.Application.Shared.Behaviors;
using System.Reflection;

namespace NotificationService.Application;

public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR — auto-registers all IRequestHandler<,> from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Pipeline Behaviors — validation runs before every handler
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // AutoMapper profiles
        services.AddAutoMapper(cfg => { }, typeof(NotificationGroupMappingProfile));

        // FluentValidation — auto-registers all AbstractValidator<T> from this assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
