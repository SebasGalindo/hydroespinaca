using ActuatorService.Application.DTOs;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IRoutineValidationService
{
    Task<List<ResolvedRoutineStepDto>> ValidateAndResolveStepsAsync(List<RoutineStepDto> steps);
}
