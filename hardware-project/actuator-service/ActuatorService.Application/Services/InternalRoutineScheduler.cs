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
/// Uses Colombia timezone (America/Bogota, UTC-5) for scheduling.
/// </summary>
public class InternalRoutineScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InternalRoutineScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeZoneInfo ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    public InternalRoutineScheduler(
        IServiceProvider serviceProvider,
        ILogger<InternalRoutineScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets current time in Colombia timezone
    /// </summary>
    private DateTime GetColombiaTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTimeZone);
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
        var nowColombia = GetColombiaTime();
        var nowUtc = DateTime.UtcNow;

        _logger.LogDebug("🔍 Checking {Count} active internal routines at {ColombiaTime} (Colombia), {UtcTime} (UTC)",
            routines.Count, nowColombia.ToString("yyyy-MM-dd HH:mm:ss"), nowUtc.ToString("yyyy-MM-dd HH:mm:ss"));

        // Determine which routines should run
        var routinesToRun = new List<InternalRoutine>();
        foreach (var routine in routines)
        {
            if (ShouldRun(routine, nowColombia, nowUtc))
            {
                routinesToRun.Add(routine);
            }
        }

        if (!routinesToRun.Any())
        {
            _logger.LogDebug("⏸️ No routines to execute at this time");
            return;
        }

        // Check for conflicts: Recirculation has priority over Aeration
        var recirculationRoutine = routinesToRun.FirstOrDefault(r => r.Name == "Recirculation");
        var aerationRoutine = routinesToRun.FirstOrDefault(r => r.Name == "Aeration");

        if (recirculationRoutine != null && aerationRoutine != null)
        {
            // Conflict detected: both should run at the same time
            _logger.LogWarning("⚠️ Routine conflict detected at {ColombiaTime}: Both Recirculation and Aeration scheduled",
                nowColombia.ToString("HH:mm"));
            _logger.LogInformation("🔝 Recirculation has priority - Aeration will be skipped");

            // Remove Aeration from execution list
            routinesToRun.Remove(aerationRoutine);

            _logger.LogWarning("⛔ Aeration skipped due to Recirculation priority at {ColombiaTime}",
                nowColombia.ToString("HH:mm"));
        }

        // Execute remaining routines
        foreach (var routine in routinesToRun)
        {
            try
            {
                var isPriorityRoutine = routine.Name == "Recirculation";
                var priorityTag = isPriorityRoutine ? " (priority routine)" : "";

                _logger.LogInformation("⏰ Triggering internal routine: {RoutineName}{PriorityTag} at {ColombiaTime}",
                    routine.Name, priorityTag, nowColombia.ToString("HH:mm"));

                await ExecuteRoutineAsync(routine, actuatorCodeResolver, commandExecutionService);

                // Update last execution time in UTC
                await repository.UpdateLastExecutedAtAsync(routine.Id, nowUtc);

                if (isPriorityRoutine)
                {
                    _logger.LogInformation("✅ Recirculation triggered successfully (priority routine) at {ColombiaTime}",
                        nowColombia.ToString("HH:mm"));
                }
                else
                {
                    _logger.LogInformation("✅ Internal routine {RoutineName} scheduled successfully at {ColombiaTime}",
                        routine.Name, nowColombia.ToString("HH:mm"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing internal routine {RoutineName} (ID: {RoutineId})",
                    routine.Name, routine.Id);
            }
        }
    }

    private bool ShouldRun(InternalRoutine routine, DateTime nowColombia, DateTime nowUtc)
    {
        // Convert last execution from UTC to Colombia time
        DateTime? lastExecutedColombia = routine.LastExecutedAt.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(routine.LastExecutedAt.Value, ColombiaTimeZone)
            : null;

        // If never executed, check if we're past the start time today
        if (lastExecutedColombia == null)
        {
            var todayStart = nowColombia.Date + routine.StartTime;
            var shouldRunFirstTime = nowColombia >= todayStart;

            if (shouldRunFirstTime)
            {
                _logger.LogDebug("⭐ Routine {RoutineName} has never run and is past start time {StartTime} (Colombia time)",
                    routine.Name, todayStart.ToString("HH:mm"));
            }

            return shouldRunFirstTime;
        }

        // Calculate time since last execution (using Colombia time)
        var timeSinceLastExecution = nowColombia - lastExecutedColombia.Value;

        // Check if enough time has passed since last execution
        // We add a small tolerance (30 seconds) to avoid missing executions due to timing precision
        var shouldRunAgain = timeSinceLastExecution >= routine.Interval.Subtract(TimeSpan.FromSeconds(30));

        if (shouldRunAgain)
        {
            _logger.LogDebug("🔄 Routine {RoutineName} interval elapsed - Last: {LastExecution} (Colombia), Interval: {Interval}, Elapsed: {Elapsed}",
                routine.Name,
                lastExecutedColombia.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                routine.Interval,
                timeSinceLastExecution.ToString(@"hh\:mm\:ss"));

            // Additional validation: ensure we're on the correct time boundary
            // For example, Recirculation (2h interval) should run at 00:00, 02:00, 04:00, etc.
            if (routine.Name == "Recirculation")
            {
                // Should run every 2 hours starting from midnight
                var hoursSinceStartTime = (nowColombia.TimeOfDay - routine.StartTime).TotalHours;
                var isOnBoundary = Math.Abs(hoursSinceStartTime % routine.Interval.TotalHours) < 0.02; // Within ~1 minute tolerance

                if (isOnBoundary)
                {
                    _logger.LogDebug("✅ Recirculation on time boundary: {CurrentTime} (Colombia)", nowColombia.ToString("HH:mm"));
                }

                return isOnBoundary;
            }
            else if (routine.Name == "Aeration")
            {
                // Should run every 30 minutes starting from midnight
                var minutesSinceStartTime = (nowColombia.TimeOfDay - routine.StartTime).TotalMinutes;
                var isOnBoundary = Math.Abs(minutesSinceStartTime % routine.Interval.TotalMinutes) < 1.5; // Within ~1.5 minute tolerance

                if (isOnBoundary)
                {
                    _logger.LogDebug("✅ Aeration on time boundary: {CurrentTime} (Colombia)", nowColombia.ToString("HH:mm"));
                }

                return isOnBoundary;
            }
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
