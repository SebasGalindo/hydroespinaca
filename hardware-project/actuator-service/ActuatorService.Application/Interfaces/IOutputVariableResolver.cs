using ActuatorService.Domain.Entities;

namespace ActuatorService.Application.Interfaces;

public interface IOutputVariableResolver
{
    /// <summary>
    /// Resolves an output variable ID to its corresponding actuator and control output
    /// </summary>
    /// <param name="outputVariableId">The control output ID</param>
    /// <returns>Tuple with control output and its associated actuator</returns>
    Task<(ControlOutput controlOutput, Actuator actuator)> ResolveAsync(string outputVariableId);
}
