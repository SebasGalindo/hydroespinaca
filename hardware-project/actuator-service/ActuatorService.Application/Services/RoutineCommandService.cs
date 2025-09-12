using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Application.Services;

public class RoutineCommandService : IRoutineCommandService
{
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly ILogger<RoutineCommandService> _logger;

    public RoutineCommandService(
        IRoutineCommandRepository routineCommandRepository,
        ILogger<RoutineCommandService> logger)
    {
        _routineCommandRepository = routineCommandRepository;
        _logger = logger;
    }

    public async Task<List<RoutineCommand>> GetAllRoutineCommandsAsync(string? esp32Id = null)
    {
        _logger.LogInformation("Getting routine commands for ESP32: {Esp32Id}", esp32Id ?? "all");
        
        return string.IsNullOrEmpty(esp32Id) 
            ? await _routineCommandRepository.GetAllAsync()
            : await _routineCommandRepository.GetByEsp32IdAsync(esp32Id);
    }

    public async Task<RoutineCommand?> GetRoutineCommandByIdAsync(string commandId)
    {
        var routineCommand = await _routineCommandRepository.GetByCommandIdAsync(commandId);

        if (routineCommand == null)
        {
            throw new NotFoundException($"Routine command with ID '{commandId}' not found");
        }

        _logger.LogInformation("Getting routine command: {CommandId}", commandId);

        return routineCommand;
    }
}