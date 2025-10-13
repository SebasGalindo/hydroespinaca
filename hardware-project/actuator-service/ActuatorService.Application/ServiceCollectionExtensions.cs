using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using ActuatorService.Application.Validators;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Actuator service
        services.AddScoped<IActuatorService, ActuatorServiceApplication>();

        // Actuator code resolution
        services.AddScoped<IActuatorCodeResolver, ActuatorCodeResolver>();

        // Command execution (new simplified flow)
        services.AddScoped<IExecuteCommandsUseCase, ExecuteCommandsUseCase>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();

        // Legacy routine support (backward compatibility for internal routines)
        services.AddScoped<IRoutineCommandService, RoutineCommandService>();
        services.AddSingleton<IRoutineExecutionService, RoutineExecutionService>();

        // Pin-based execution (singleton for in-memory state and locking)
        services.AddSingleton<IPinLockRegistry, PinLockRegistry>();

        // Actuator state machine (singleton for in-memory state)
        services.AddSingleton<IActuatorStateMachine, ActuatorStateMachine>();
        services.AddScoped<ActuatorStartupSyncService>();

        // Advanced behavior rules (refactored to work with actuator codes)
        services.AddScoped<AdvancedBehaviorRulesService>();

        // Validators
        services.AddScoped<IValidator<ExecuteCommandsDto>, ExecuteCommandsValidator>();
        services.AddScoped<IValidator<ActuatorControlDto>, ActuatorControlValidator>();

        return services;
    }
}
