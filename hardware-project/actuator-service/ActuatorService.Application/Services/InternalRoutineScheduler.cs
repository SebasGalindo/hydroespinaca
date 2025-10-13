using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Background service that schedules and executes internal recurring routines.
/// Checks every minute if any routine should be triggered based on their interval and last execution time.
/// </summary>
public class InternalRoutineScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InternalRoutineScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public InternalRoutineScheduler(
        IServiceProvider serviceProvider,
        ILogger<InternalRoutineScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕐 Internal Routine Scheduler started");

        // Wait a bit on startup to ensure other services are ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndTriggerRoutinesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in internal routine scheduler cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Internal Routine Scheduler stopped");
    }

    private async Task CheckAndTriggerRoutinesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInternalRoutineRepository>();
        var actuatorCodeResolver = scope.ServiceProvider.GetRequiredService<IActuatorCodeResolver>();
        var commandExecutionService = scope.ServiceProvider.GetRequiredService<ICommandExecutionService>();

        var routines = await repository.GetActiveRoutinesAsync();
        var now = DateTime.UtcNow;

        _logger.LogDebug("🔍 Checking {Count} active internal routines", routines.Count);

        foreach (var routine in routines)
        {
            try
            {
                if (ShouldRun(routine, now))
                {
                    _logger.LogInformation("⏰ Triggering internal routine: {RoutineName} (ID: {RoutineId})",
                        routine.Name, routine.Id);

                    await ExecuteRoutineAsync(routine, actuatorCodeResolver, commandExecutionService);

                    // Update last execution time
                    await repository.UpdateLastExecutedAtAsync(routine.Id, now);

                    _logger.LogInformation("✅ Internal routine {RoutineName} scheduled successfully", routine.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing internal routine {RoutineName} (ID: {RoutineId})",
                    routine.Name, routine.Id);
            }
        }
    }

    private bool ShouldRun(InternalRoutine routine, DateTime now)
    {
        // If never executed, check if we're past the start time today
        if (routine.LastExecutedAt == null)
        {
            var todayStart = now.Date + routine.StartTime;
            var shouldRunFirstTime = now >= todayStart;

            if (shouldRunFirstTime)
            {
                _logger.LogDebug("⭐ Routine {RoutineName} has never run and is past start time", routine.Name);
            }

            return shouldRunFirstTime;
        }

        // Check if enough time has passed since last execution
        var timeSinceLastExecution = now - routine.LastExecutedAt.Value;
        var shouldRunAgain = timeSinceLastExecution >= routine.Interval;

        if (shouldRunAgain)
        {
            _logger.LogDebug("🔄 Routine {RoutineName} interval elapsed (last: {LastExecution}, interval: {Interval})",
                routine.Name, routine.LastExecutedAt, routine.Interval);
        }

        return shouldRunAgain;
    }

    private async Task ExecuteRoutineAsync(
        InternalRoutine routine,
        IActuatorCodeResolver actuatorCodeResolver,
        ICommandExecutionService commandExecutionService)
    {
        // Convert InternalRoutineStep to individual commands
        // OutputVariable now contains ActuatorCode (e.g., "BombaRiego", "Ventiladores")
        var resolvedCommands = new List<ResolvedCommandDto>();

        foreach (var step in routine.Steps)
        {
            try
            {
                // Resolve actuator by code (OutputVariable now stores ActuatorCode)
                var actuator = await actuatorCodeResolver.ResolveAsync(step.OutputVariable);

                if (actuator == null)
                {
                    _logger.LogWarning("⚠️ Actuator {ActuatorCode} not found for routine {RoutineName}, skipping step",
                        step.OutputVariable, routine.Name);
                    continue;
                }

                // Validate that actuator belongs to the configured ESP32
                if (actuator.Esp32Id != routine.Esp32Id)
                {
                    _logger.LogWarning("⚠️ Actuator {ActuatorCode} belongs to ESP32 {ActualEsp32}, but routine {RoutineName} is configured for ESP32 {ConfiguredEsp32}",
                        step.OutputVariable, actuator.Esp32Id, routine.Name, routine.Esp32Id);
                    continue;
                }

                resolvedCommands.Add(new ResolvedCommandDto
                {
                    ActuatorCode = step.OutputVariable,
                    ActuatorId = actuator.Id,
                    Esp32Id = actuator.Esp32Id,
                    Pin = actuator.Pin,
                    Mode = actuator.Mode,
                    Power = step.Power,
                    DutyCycle = step.DutyCycle,
                    Duration = step.Duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resolving actuator {ActuatorCode} for routine {RoutineName}",
                    step.OutputVariable, routine.Name);
            }
        }

        if (resolvedCommands.Count == 0)
        {
            throw new InvalidOperationException($"Internal routine '{routine.Name}' has no valid commands after resolution");
        }

        // Schedule individual commands (respects pin locks and queuing)
        await commandExecutionService.ScheduleCommandsAsync(resolvedCommands, routine.Esp32Id);

        _logger.LogDebug("📤 Scheduled {CommandCount} commands for internal routine {RoutineName}",
            resolvedCommands.Count, routine.Name);
    }
}
