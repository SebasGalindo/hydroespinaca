using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Errors;

namespace ActuatorService.Application.Services;

public interface IMqttPayloadEnrichmentService
{
    Task<MqttRoutinePayloadDto> EnrichPayloadAsync(string commandId, List<RoutineStepDto> steps);
}

public class MqttPayloadEnrichmentService : IMqttPayloadEnrichmentService
{
    private readonly IActuatorRepository _actuatorRepository;

    public MqttPayloadEnrichmentService(IActuatorRepository actuatorRepository)
    {
        _actuatorRepository = actuatorRepository;
    }

    public async Task<MqttRoutinePayloadDto> EnrichPayloadAsync(string commandId, List<RoutineStepDto> steps)
    {
        var actuatorIds = steps.Select(s => s.Actuator).Distinct().ToList();
        var actuators = await _actuatorRepository.GetByIdsAsync(actuatorIds);

        var missingActuators = actuatorIds.Except(actuators.Select(a => a.Id)).ToList();
        if (missingActuators.Any())
        {
            throw new NotFoundException($"Actuators not found: {string.Join(", ", missingActuators)}");
        }

        var actuatorMap = actuators.ToDictionary(a => a.Id);

        var mqttSteps = steps.Select(step =>
        {
            var actuator = actuatorMap[step.Actuator];
            
            return new MqttStepDto
            {
                Pin = actuator.Pin,
                Mode = actuator.Mode.ToString(),
                Power = step.Power,
                DutyCycle = step.DutyCycle,
                Duration = step.Duration
            };
        }).ToList();

        return new MqttRoutinePayloadDto
        {
            RoutineId = commandId,
            Steps = mqttSteps
        };
    }
}