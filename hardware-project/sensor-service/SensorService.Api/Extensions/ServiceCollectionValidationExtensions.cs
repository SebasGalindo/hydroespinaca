using FluentValidation;
using SensorService.Application.Validators.Esp32Node;

namespace SensorService.Api.Extensions;

/// <summary>
/// Extension methods for registering FluentValidation validators in the DI container.
/// </summary>
public static class ServiceCollectionValidationExtensions
{
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Esp32NodeCreateValidator>();
        return services;
    }
}
