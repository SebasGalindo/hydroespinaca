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
        var routineExecutionService = scope.ServiceProvider.GetRequiredService<IRoutineExecutionService>();
        var stateMachine = scope.ServiceProvider.GetRequiredService<IActuatorStateMachine>();

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

            // Notify execution service that routine completed
            // This will:
            // 1. Release pin locks
            // 2. Update actuator states to OFF
            // 3. Activate pending routines that were waiting for these pins
            await routineExecutionService.OnRoutineCompletedAsync(completion.CommandId);

            _logger.LogInformation("✅ Processed completion for routine {CommandId} with status: {Status}",
                completion.CommandId, completion.Status);
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