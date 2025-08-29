using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttRoutineCompletionSubscriber
{
    private readonly IMqttClientService _mqttClient;
    private readonly IRoutineCommandRepository _routineCommandRepository;
    private readonly ILogger<MqttRoutineCompletionSubscriber> _logger;
    private const string COMPLETION_TOPIC = "actuator/routine/completions";

    public MqttRoutineCompletionSubscriber(
        IMqttClientService mqttClient,
        IRoutineCommandRepository routineCommandRepository,
        ILogger<MqttRoutineCompletionSubscriber> logger)
    {
        _mqttClient = mqttClient;
        _routineCommandRepository = routineCommandRepository;
        _logger = logger;
    }

    public async Task StartListeningAsync()
    {
        await _mqttClient.SubscribeAsync(COMPLETION_TOPIC, OnRoutineCompletionReceived);
        _logger.LogInformation("Started listening for routine completions on topic: {Topic}", COMPLETION_TOPIC);
    }

    private async Task OnRoutineCompletionReceived(string topic, string payload)
    {
        try
        {
            var completion = JsonSerializer.Deserialize<RoutineCompletionDto>(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (completion == null)
            {
                _logger.LogWarning("Received null completion data from topic: {Topic}", topic);
                return;
            }

            var routineCommand = await _routineCommandRepository.GetByCommandIdAsync(completion.RoutineId);
            
            if (routineCommand == null)
            {
                _logger.LogWarning("Routine command not found for RoutineId: {RoutineId}", completion.RoutineId);
                return;
            }

            // Update the routine command with completion data
            routineCommand.StatusGeneral = Enum.Parse<RoutineCommandStatus>(completion.Status);
            routineCommand.FinishedAt = completion.FinishedAt;
            routineCommand.Results = completion.Results?.Select(r => new Domain.Entities.RoutineResult
            {
                Pin = r.Pin,
                Status = r.Status
            }).ToList();
            routineCommand.ExecutionLogs = completion.Logs;

            await _routineCommandRepository.UpdateAsync(routineCommand);

            _logger.LogInformation("Updated routine command {CommandId} with status: {Status}", 
                completion.RoutineId, completion.Status);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize routine completion from topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing routine completion from topic: {Topic}", topic);
        }
    }
}