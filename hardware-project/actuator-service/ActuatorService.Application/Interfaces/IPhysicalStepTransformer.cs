using ActuatorService.Application.DTOs;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Interfaces;

public interface IPhysicalStepTransformer
{
    MqttStepDto TransformToPhysicalStep(ResolvedRoutineStepDto resolvedStep);
}
