using AuthService.Application.Features.Users.Mappings;
using AuthService.Application.Mappers;
using AuthService.Application.Shared.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AuthService.Application;

/// <summary>
/// Extension methods for registering Auth Service application layer dependencies (MediatR, FluentValidation, AutoMapper).
/// </summary>
public static class ServiceCollectionApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Pipeline Behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // AutoMapper
        services.AddAutoMapper(cfg => { },
             typeof(Features.Users.Mappings.UserMappingProfile),
             typeof(Features.Roles.Mappings.RoleMappingProfile),
             typeof(Features.Permissions.Mappings.PermissionMappingProfile),
             typeof(RefreshMappingProfile),
             typeof(DomainMappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
