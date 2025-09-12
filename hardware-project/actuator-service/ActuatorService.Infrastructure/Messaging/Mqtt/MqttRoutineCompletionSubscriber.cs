using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttRoutineCompletionSubscriber : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttClient;
    private readonly ILogger<MqttRoutineCompletionSubscriber> _logger;
    private const string COMPLETION_TOPIC = MqttTopics.Actuator.RoutineCompletions;

    public MqttRoutineCompletionSubscriber(
        IServiceProvider serviceProvider,
        IMqttClientService mqttClient,
        ILogger<MqttRoutineCompletionSubscriber> logger)
    {
        _serviceProvider = serviceProvider;
        _mqttClient = mqttClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Starting MQTT Routine Completion Subscriber...");

        try
        {
            await _mqttClient.SubscribeAsync(COMPLETION_TOPIC, async (topic, payload) =>
            {
                await OnRoutineCompletionReceived(topic, payload, stoppingToken);
            });

            _logger.LogInformation("✅ MQTT Routine Completion Subscriber started and subscribed to topic: {Topic}", COMPLETION_TOPIC);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("📄 MQTT Routine Completion Subscriber is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in MQTT Routine Completion Subscriber");
            throw;
        }
    }

    private async Task OnRoutineCompletionReceived(string topic, string payload, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        // Only process messages from the completion topic
        if (topic != COMPLETION_TOPIC)
        {
            _logger.LogDebug("🚫 Ignoring message from topic: {Topic} (expected: {ExpectedTopic})", topic, COMPLETION_TOPIC);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var routineCommandRepository = scope.ServiceProvider.GetRequiredService<IRoutineCommandRepository>();
        var jobScheduleStateManager = scope.ServiceProvider.GetRequiredService<IJobScheduleStateManager>();

        try
        {
            _logger.LogDebug("📨 Received routine completion on topic: {Topic}", topic);

            var completion = JsonSerializer.Deserialize<RoutineCompletionDto>(payload, JsonConstants.SerializerOptions.CamelCase);

            if (completion == null)
            {
                _logger.LogWarning("⚠️ Received null completion data from topic: {Topic}", topic);
                return;
            }
            Console.WriteLine($"Received routine completion: {JsonSerializer.Serialize(completion, JsonConstants.SerializerOptions.CamelCase)}");
            
            // Update routine command in database
            var routineCommand = await routineCommandRepository.GetByCommandIdAsync(completion.CommandId);
            
            if (routineCommand == null)
            {
                _logger.LogWarning("⚠️ Routine command not found for CommandId: {CommandId}", completion.CommandId);
                return;
            }

            // Update the routine command with completion data
            routineCommand.StatusGeneral = completion.Status;
            routineCommand.FinishedAt = completion.FinishedAt;
            routineCommand.Results = completion.Steps?.Select(step => new Domain.Entities.RoutineResult
            {
                Pin = step.Pin.ToString(),
                Status = step.Status,
                ExecutionLog = step.ExecutionLog
            }).ToList();
            

            await routineCommandRepository.UpdateAsync(routineCommand);

            // Remove completed routine from in-memory job schedule
            jobScheduleStateManager.RemoveCompletedRoutine(completion.CommandId);
            
            // Mark the next command in the same channel as running (if exists) in both memory and database
            if (routineCommand.Channel.HasValue)
            {
                jobScheduleStateManager.MarkNextCommandAsRunning(completion.Esp32Id, routineCommand.Channel.Value);
                
                // Also update the next command in database to IN_PROGRESS
                var jobStatus = jobScheduleStateManager.GetJobStatus(completion.Esp32Id);
                var channel = jobStatus.Channels.FirstOrDefault(c => c.Channel == routineCommand.Channel.Value);
                var nextCommand = channel?.Queue.FirstOrDefault(q => q.Status == ActuatorConstants.CommandStatuses.Running);
                
                if (nextCommand != null)
                {
                    var nextCommandEntity = await routineCommandRepository.GetByCommandIdAsync(nextCommand.CommandId);
                    if (nextCommandEntity != null && nextCommandEntity.StatusGeneral != RoutineCommandStatus.IN_PROGRESS)
                    {
                        nextCommandEntity.StatusGeneral = RoutineCommandStatus.IN_PROGRESS;
                        await routineCommandRepository.UpdateAsync(nextCommandEntity);
                        _logger.LogInformation("🚀 Updated next command {CommandId} to IN_PROGRESS in database", nextCommand.CommandId);
                    }
                }
            }

            _logger.LogInformation("✅ Updated routine command {CommandId} with status: {Status} for ESP32 {Esp32Id}, removed from job schedule and marked next command as running", 
                completion.CommandId, completion.Status, completion.Esp32Id);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to deserialize routine completion from topic: {Topic}, Payload: {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing routine completion from topic: {Topic}", topic);
        }
    }
}