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
/// Uses deterministic scheduling based on 00:00 Colombia time without persisted state.
/// Checks every minute if any routine should be triggered.
/// </summary>
public class InternalRoutineScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InternalRoutineScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeZoneInfo ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    // Track last execution time in memory to avoid duplicate triggers within the same minute
    private readonly Dictionary<string, DateTime> _lastExecutions = new();
    private readonly object _lock = new();

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

    /// <summary>
    /// Calculates the next execution time for a routine based on its interval.
    /// Pure function - always returns the same result for the same inputs.
    /// </summary>
    /// <param name="interval">Routine interval (e.g., 02:00:00 for 2 hours)</param>
    /// <param name="timezoneId">Timezone ID (default: America/Bogota)</param>
    /// <returns>Next execution time in UTC</returns>
    public static DateTime GetNextExecution(TimeSpan interval, string timezoneId = "America/Bogota")
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var baseTime = nowLocal.Date; // 00:00 of today in local time
        var elapsed = nowLocal - baseTime;

        // Calculate how many intervals have passed since midnight
        var intervalsPassed = (long)(elapsed.Ticks / interval.Ticks);

        // Next execution is the next interval boundary
        var next = baseTime + TimeSpan.FromTicks((intervalsPassed + 1) * interval.Ticks);

        return TimeZoneInfo.ConvertTimeToUtc(next, tz);
    }

    /// <summary>
    /// Determines if a routine should execute now based on deterministic calculation from 00:00
    /// </summary>
    private bool ShouldExecuteNow(InternalRoutine routine, DateTime nowColombia)
    {
        var baseTime = nowColombia.Date; // 00:00 today
        var elapsed = nowColombia - baseTime;

        // Calculate minutes since midnight
        var minutesSinceMidnight = (int)elapsed.TotalMinutes;
        var intervalMinutes = (int)routine.Interval.TotalMinutes;

        // Check if we're on an interval boundary (within 1-minute tolerance)
        var isOnBoundary = minutesSinceMidnight % intervalMinutes == 0;

        if (!isOnBoundary)
            return false;

        // Check if we already executed this routine in the last 2 minutes (avoid duplicates)
        lock (_lock)
        {
            if (_lastExecutions.TryGetValue(routine.Id, out var lastExec))
            {
                var timeSinceLast = nowColombia - lastExec;
                if (timeSinceLast < TimeSpan.FromMinutes(2))
                {
                    _logger.LogDebug("⏭️ Skipping {RoutineName} - already executed {Seconds}s ago",
                        routine.Name, (int)timeSinceLast.TotalSeconds);
                    return false;
                }
            }
        }

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕐 Internal Routine Scheduler started (deterministic mode, timezone: America/Bogota)");

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

        _logger.LogDebug("🔍 Checking {Count} active internal routines at {ColombiaTime} (Colombia)",
            routines.Count, nowColombia.ToString("yyyy-MM-dd HH:mm:ss"));

        // Determine which routines should run based on deterministic calculation
        var routinesToRun = new List<InternalRoutine>();
        foreach (var routine in routines)
        {
            if (ShouldExecuteNow(routine, nowColombia))
            {
                routinesToRun.Add(routine);

                var nextExec = GetNextExecution(routine.Interval);
                var nextExecColombia = TimeZoneInfo.ConvertTimeFromUtc(nextExec, ColombiaTimeZone);

                _logger.LogDebug("📅 {RoutineName} scheduled to run now. Next execution: {NextTime} (Colombia)",
                    routine.Name, nextExecColombia.ToString("HH:mm"));
            }
        }

        if (!routinesToRun.Any())
        {
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

                // Mark as executed in memory
                lock (_lock)
                {
                    _lastExecutions[routine.Id] = nowColombia;
                }

                // Calculate and log next execution
                var nextExec = GetNextExecution(routine.Interval);
                var nextExecColombia = TimeZoneInfo.ConvertTimeFromUtc(nextExec, ColombiaTimeZone);

                if (isPriorityRoutine)
                {
                    _logger.LogInformation("✅ Recirculation triggered successfully (priority routine) at {ColombiaTime}. Next: {NextTime}",
                        nowColombia.ToString("HH:mm"), nextExecColombia.ToString("HH:mm"));
                }
                else
                {
                    _logger.LogInformation("✅ Internal routine {RoutineName} scheduled successfully at {ColombiaTime}. Next: {NextTime}",
                        routine.Name, nowColombia.ToString("HH:mm"), nextExecColombia.ToString("HH:mm"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing internal routine {RoutineName} (ID: {RoutineId})",
                    routine.Name, routine.Id);
            }
        }
    }

    private async Task ExecuteRoutineAsync(
        InternalRoutine routine,
        IActuatorCodeResolver actuatorCodeResolver,
        ICommandExecutionService commandExecutionService)
    {
        using var scope = _serviceProvider.CreateScope();
        var routineCommandRepository = scope.ServiceProvider.GetRequiredService<IRoutineCommandRepository>();

        // Convert InternalRoutineStep to individual commands
        var resolvedCommands = new List<ResolvedCommandDto>();

        foreach (var step in routine.Steps)
        {
            try
            {
                // Resolve actuator by code
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

        // Persist commands to database (same logic as ExecuteCommandsUseCase)
        foreach (var resolvedCommand in resolvedCommands)
        {
            var power = resolvedCommand.Power ?? resolvedCommand.DutyCycle?.ToString();
            var isPowerOff = power?.Equals(HydroEspinaca.Shared.Constants.ActuatorConstants.PowerStates.Off, StringComparison.OrdinalIgnoreCase) == true;

            // Check for existing RUNNING command for this actuator
            var existingRunningCommand = await routineCommandRepository.GetRunningByActuatorCodeAsync(resolvedCommand.ActuatorCode);

            if (existingRunningCommand != null)
            {
                // Update existing RUNNING command (extend it)
                // Note: We only update ExtendedAt timestamp. Power/Duration are not stored.
                // The MQTT message will contain the new parameters for the firmware.
                existingRunningCommand.ExtendedAt = DateTime.UtcNow;

                await routineCommandRepository.UpdateAsync(existingRunningCommand);

                _logger.LogInformation("🔄 Extended existing RUNNING command for {ActuatorCode} from routine {RoutineName}",
                    resolvedCommand.ActuatorCode, routine.Name);
            }
            else if (!isPowerOff)
            {
                // Create new RUNNING command (only if not OFF)
                var commandId = $"{resolvedCommand.ActuatorCode}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                var routineCommandEntity = new RoutineCommand
                {
                    CommandId = commandId,
                    ActuatorCode = resolvedCommand.ActuatorCode,
                    Esp32Id = routine.Esp32Id,
                    StatusGeneral = HydroEspinaca.Shared.Enums.RoutineCommandStatus.RUNNING,
                    CreatedAt = DateTime.UtcNow
                };

                await routineCommandRepository.AddAsync(routineCommandEntity);

                _logger.LogInformation("💾 Created RUNNING command for {ActuatorCode} from routine {RoutineName}",
                    resolvedCommand.ActuatorCode, routine.Name);
            }
        }

        // Schedule individual commands (respects pin locks and queuing)
        await commandExecutionService.ScheduleCommandsAsync(resolvedCommands, routine.Esp32Id);

        _logger.LogDebug("📤 Scheduled {CommandCount} commands for internal routine {RoutineName}",
            resolvedCommands.Count, routine.Name);
    }
}
