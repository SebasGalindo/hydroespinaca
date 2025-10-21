using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;

namespace ActuatorService.Application.Services;

/// <summary>
/// Resolves actuator codes to physical actuators
/// Replaces OutputVariableResolver after removing ControlOutputs
/// </summary>
public interface IActuatorCodeResolver
{
    Task<Actuator> ResolveAsync(string actuatorCode);
}

public class ActuatorCodeResolver : IActuatorCodeResolver
{
    private readonly IActuatorRepository _actuatorRepository;

    public ActuatorCodeResolver(IActuatorRepository actuatorRepository)
    {
        _actuatorRepository = actuatorRepository;
    }

    public async Task<Actuator> ResolveAsync(string actuatorCode)
    {
        var allActuators = await _actuatorRepository.GetAllAsync();
        var actuator = allActuators.FirstOrDefault(a => a.Code == actuatorCode);

        if (actuator == null)
        {
            throw new ActuatorNotFoundException($"Actuator with code '{actuatorCode}' not found");
        }

        return actuator;
    }
}
