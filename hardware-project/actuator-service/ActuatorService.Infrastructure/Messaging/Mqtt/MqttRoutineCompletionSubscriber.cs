using ActuatorService.Application.Interfaces;
using ActuatorService.Application.Services;
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
        var commandExecutionService = scope.ServiceProvider.GetRequiredService<ICommandExecutionService>();

        try
        {
            _logger.LogDebug("📨 Received command completion on topic: {Topic}", topic);

            var completion = JsonSerializer.Deserialize<RoutineCompletionDto>(payload, JsonConstants.SerializerOptions.CamelCase);

            if (completion == null)
            {
                _logger.LogWarning("⚠️ Received null completion data from topic: {Topic}", topic);
                return;
            }

            _logger.LogDebug("📦 Received command completion: CommandId={CommandId}, Status={Status}, Steps={StepCount}",
                completion.CommandId, completion.Status, completion.Steps?.Count ?? 0);

            // Process command completion (releases pin, updates state, activates pending)
            await commandExecutionService.OnCommandCompletedAsync(completion.CommandId);

            _logger.LogInformation("✅ Processed command completion for {CommandId} with status: {Status}",
                completion.CommandId, completion.Status);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("⚠️ Command {CommandId} not found in active commands",
                JsonSerializer.Deserialize<RoutineCompletionDto>(payload, JsonConstants.SerializerOptions.CamelCase)?.CommandId ?? "unknown");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to deserialize command completion from topic: {Topic}, Payload: {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing command completion from topic: {Topic}", topic);
        }
    }
}