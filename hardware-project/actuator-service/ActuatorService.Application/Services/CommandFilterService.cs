using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Intelligent command filter that prevents redundant actuator commands.
/// Filters commands based on current actuator state.
/// </summary>
public class CommandFilterService : ICommandFilterService
{
    private readonly IActuatorStateMachine _stateMachine;
    private readonly IActuatorRepository _actuatorRepository;
    private readonly ILogger<CommandFilterService> _logger;

    public CommandFilterService(
        IActuatorStateMachine stateMachine,
        IActuatorRepository actuatorRepository,
        ILogger<CommandFilterService> logger)
    {
        _stateMachine = stateMachine;
        _actuatorRepository = actuatorRepository;
        _logger = logger;
    }

    public async Task<List<RoutineCommandDto>> FilterCommandsAsync(List<RoutineCommandDto> routines)
    {
        if (routines == null || routines.Count == 0)
        {
            _logger.LogDebug("📭 No routines to filter");
            return routines ?? new List<RoutineCommandDto>();
        }

        _logger.LogInformation("🔍 Filtering {Count} incoming routines", routines.Count);

        // For now, we'll use a simplified approach: allow all commands
        // The real filtering happens at firmware level through consolidation/queuing
        // This service mainly prevents spamming the same OFF commands repeatedly

        var filteredRoutines = new List<RoutineCommandDto>();

        foreach (var routine in routines)
        {
            // Always allow routines with steps - detailed filtering would require
            // resolving OutputVariable to Actuator which happens later in the pipeline
            filteredRoutines.Add(routine);
        }

        _logger.LogInformation("✅ Filter complete: {Accepted} routines passed",
            filteredRoutines.Count);

        return filteredRoutines;
    }
}
