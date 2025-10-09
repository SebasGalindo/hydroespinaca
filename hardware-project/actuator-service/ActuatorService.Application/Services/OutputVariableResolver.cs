using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Exceptions;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ActuatorService.Application.Services;

public class OutputVariableResolver : IOutputVariableResolver
{
    private readonly IControlOutputRepository _controlOutputRepo;
    private readonly IActuatorRepository _actuatorRepo;
    private readonly ILogger<OutputVariableResolver> _logger;

    public OutputVariableResolver(
        IControlOutputRepository controlOutputRepo,
        IActuatorRepository actuatorRepo,
        ILogger<OutputVariableResolver> logger)
    {
        _controlOutputRepo = controlOutputRepo;
        _actuatorRepo = actuatorRepo;
        _logger = logger;
    }

    public async Task<(ControlOutput controlOutput, Actuator actuator)> ResolveAsync(string outputVariableId)
    {
        // Validate ObjectId format
        if (!ObjectId.TryParse(outputVariableId, out _))
        {
            _logger.LogWarning("Invalid outputVariable format: {OutputVariableId}", outputVariableId);
            throw new ArgumentException(
                $"Invalid outputVariable format. Expected ObjectId hex string of {ActuatorConstants.Validation.ObjectIdLength} characters.",
                nameof(outputVariableId));
        }

        // Get control output
        var controlOutput = await _controlOutputRepo.GetByIdAsync(outputVariableId);
        if (controlOutput == null)
        {
            _logger.LogWarning("Control output not found: {OutputVariableId}", outputVariableId);
            throw new ControlOutputNotFoundException(outputVariableId);
        }

        // Get associated actuator
        var actuator = await _actuatorRepo.GetByIdAsync(controlOutput.ActuatorId);
        if (actuator == null)
        {
            _logger.LogError(
                "Actuator {ActuatorId} referenced by control output {OutputVariableId} not found",
                controlOutput.ActuatorId,
                outputVariableId);
            throw new ActuatorNotFoundException(controlOutput.ActuatorId);
        }

        _logger.LogDebug(
            "Resolved outputVariable {OutputVariableId} to actuator {ActuatorId} (ESP32: {Esp32Id}, Pin: {Pin}, Mode: {Mode})",
            outputVariableId,
            actuator.Id,
            actuator.Esp32Id,
            actuator.Pin,
            actuator.Mode);

        return (controlOutput, actuator);
    }
}
