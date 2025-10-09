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
        var routineValidationService = scope.ServiceProvider.GetRequiredService<IRoutineValidationService>();
        var routineExecutionService = scope.ServiceProvider.GetRequiredService<IRoutineExecutionService>();

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

                    await ExecuteRoutineAsync(routine, routineValidationService, routineExecutionService);

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
        IRoutineValidationService validationService,
        IRoutineExecutionService executionService)
    {
        // Convert InternalRoutineStep to RoutineStepDto (same format as fuzzy-service)
        var steps = routine.Steps.Select(step => new RoutineStepDto
        {
            OutputVariable = step.OutputVariable,
            Power = step.Power,
            Duration = step.Duration,
            DutyCycle = step.DutyCycle
        }).ToList();

        // Validate and resolve steps (OutputVariable -> ControlOutput -> Actuator)
        var resolvedSteps = await validationService.ValidateAndResolveStepsAsync(steps);

        // Ensure all steps belong to the same ESP32
        var esp32Ids = resolvedSteps.Select(s => s.Esp32Id).Distinct().ToList();
        if (esp32Ids.Count > 1)
        {
            throw new InvalidOperationException(
                $"Internal routine '{routine.Name}' contains steps from multiple ESP32 devices: {string.Join(", ", esp32Ids)}");
        }

        if (esp32Ids.Count() == 0)
        {
            throw new InvalidOperationException($"Internal routine '{routine.Name}' has no valid steps");
        }

        var esp32Id = esp32Ids.First();

        // Verify it matches the configured ESP32
        if (esp32Id != routine.Esp32Id)
        {
            _logger.LogWarning("⚠️ Routine {RoutineName} configured for ESP32 {ConfiguredEsp32} but resolved to {ActualEsp32}",
                routine.Name, routine.Esp32Id, esp32Id);
        }

        var resolvedRoutine = new ResolvedRoutineDto
        {
            RoutineId = $"internal_{routine.Name.ToLower()}",
            Esp32Id = esp32Id,
            ResolvedSteps = resolvedSteps
        };

        // Schedule using the same execution service (respects pin locks)
        await executionService.ScheduleRoutinesAsync(new List<ResolvedRoutineDto> { resolvedRoutine }, esp32Id);
    }
}
