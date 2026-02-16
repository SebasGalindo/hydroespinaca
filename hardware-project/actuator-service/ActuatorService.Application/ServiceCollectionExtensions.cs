using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using ActuatorService.Application.Validators;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Application;

/// <summary>
/// Extension methods for registering Actuator Service application layer dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Actuator service
        services.AddScoped<IActuatorService, ActuatorServiceApplication>();

        // Actuator code resolution
        services.AddScoped<IActuatorCodeResolver, ActuatorCodeResolver>();

        // Command execution
        services.AddScoped<IExecuteCommandsUseCase, ExecuteCommandsUseCase>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();

        // Analytics
        services.AddScoped<IGetActuatorAnalyticsUseCase, GetActuatorAnalyticsUseCase>();

        // Routine command service (for querying executed commands)
        services.AddScoped<IRoutineCommandService, RoutineCommandService>();

        // Pin-based execution (singleton for in-memory state and locking)
        services.AddSingleton<IPinLockRegistry, PinLockRegistry>();

        // Actuator state machine (singleton for in-memory state)
        services.AddSingleton<IActuatorStateMachine, ActuatorStateMachine>();
        services.AddScoped<ActuatorStartupSyncService>();

        // Water-related actuators lock service (singleton for in-memory state)
        services.AddSingleton<IWaterRelatedActuatorsLockService, WaterRelatedActuatorsLockService>();

        // Advanced behavior rules (refactored to work with actuator codes)
        services.AddScoped<AdvancedBehaviorRulesService>();

        // Internal routine scheduler (background service for time-based routines)
        services.AddHostedService<InternalRoutineScheduler>();

        // Validators
        services.AddScoped<IValidator<ExecuteCommandsDto>, ExecuteCommandsValidator>();
        services.AddScoped<IValidator<ActuatorControlDto>, ActuatorControlValidator>();

        return services;
    }
}
