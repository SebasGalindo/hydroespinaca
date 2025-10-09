using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
using ActuatorService.Application.UseCases;
using ActuatorService.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ActuatorService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IActuatorService, ActuatorServiceApplication>();
        services.AddScoped<IControlOutputService, ControlOutputServiceApplication>();

        // Output variable resolution
        services.AddScoped<IOutputVariableResolver, OutputVariableResolver>();
        services.AddScoped<IPhysicalStepTransformer, PhysicalStepTransformer>();

        // Routine command services
        services.AddScoped<IRoutineCommandService, RoutineCommandService>();
        services.AddScoped<IRoutineValidationService, RoutineValidationService>();
        services.AddScoped<ICommandIdGenerator, CommandIdGenerator>();
        services.AddScoped<IMqttPayloadEnrichmentService, MqttPayloadEnrichmentService>();

        // Multi-routine command services
        services.AddScoped<IExecuteMultiRoutineCommandUseCase, ExecuteMultiRoutineCommandUseCase>();

        // Pin-based execution (singleton for in-memory state and locking)
        services.AddSingleton<IPinLockRegistry, PinLockRegistry>();
        services.AddSingleton<IRoutineExecutionService, RoutineExecutionService>();

        // Actuator state machine (singleton for in-memory state)
        services.AddSingleton<IActuatorStateMachine, ActuatorStateMachine>();
        services.AddScoped<ActuatorStartupSyncService>();

        // Command filtering
        services.AddScoped<ICommandFilterService, CommandFilterService>();

        // Advanced behavior rules
        services.AddScoped<AdvancedBehaviorRulesService>();

        return services;
    }
}
