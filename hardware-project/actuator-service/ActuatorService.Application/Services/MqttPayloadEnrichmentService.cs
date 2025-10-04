using ActuatorService.Application.DTOs;
using ActuatorService.Application.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;

namespace ActuatorService.Application.Services;

public interface IMqttPayloadEnrichmentService
{
    MqttRoutinePayloadDto CreatePhysicalPayload(string commandId, List<ResolvedRoutineStepDto> resolvedSteps);
}

public class MqttPayloadEnrichmentService : IMqttPayloadEnrichmentService
{
    private readonly IPhysicalStepTransformer _physicalStepTransformer;

    public MqttPayloadEnrichmentService(IPhysicalStepTransformer physicalStepTransformer)
    {
        _physicalStepTransformer = physicalStepTransformer;
    }

    public MqttRoutinePayloadDto CreatePhysicalPayload(string commandId, List<ResolvedRoutineStepDto> resolvedSteps)
    {
        var mqttSteps = resolvedSteps
            .Select(step => _physicalStepTransformer.TransformToPhysicalStep(step))
            .ToList();

        return new MqttRoutinePayloadDto
        {
            RoutineId = commandId,
            Steps = mqttSteps
        };
    }
}