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

/// <summary>
/// MQTT subscriber that listens for routine completion acknowledgments from ESP32 nodes.
/// </summary>
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
        var routineCommandRepository = scope.ServiceProvider.GetRequiredService<IRoutineCommandRepository>();

        try
        {
            _logger.LogDebug("📨 Received command completion on topic: {Topic}", topic);

            // Try to detect if this is a batch or individual completion
            // Batch format: { "completions": [ {...}, {...} ] }
            // Individual format: { "esp32Id": "...", "commandId": "...", "status": "..." }

            List<RoutineCompletionDto> completions;

            // Try to deserialize as batch first
            try
            {
                var batch = JsonSerializer.Deserialize<RoutineCompletionBatchDto>(payload, JsonConstants.SerializerOptions.CamelCase);
                if (batch?.Completions != null && batch.Completions.Count > 0)
                {
                    completions = batch.Completions;
                    _logger.LogInformation("📦 Received BATCH completion with {Count} commands", completions.Count);
                }
                else
                {
                    // Not a batch, try individual
                    var singleCompletion = JsonSerializer.Deserialize<RoutineCompletionDto>(payload, JsonConstants.SerializerOptions.CamelCase);
                    if (singleCompletion == null)
                    {
                        _logger.LogWarning("⚠️ Received null completion data from topic: {Topic}", topic);
                        return;
                    }
                    completions = new List<RoutineCompletionDto> { singleCompletion };
                    _logger.LogDebug("📦 Received INDIVIDUAL completion: CommandId={CommandId}", singleCompletion.CommandId);
                }
            }
            catch (JsonException)
            {
                // Fallback: try individual format
                var singleCompletion = JsonSerializer.Deserialize<RoutineCompletionDto>(payload, JsonConstants.SerializerOptions.CamelCase);
                if (singleCompletion == null)
                {
                    _logger.LogWarning("⚠️ Received null completion data from topic: {Topic}", topic);
                    return;
                }
                completions = new List<RoutineCompletionDto> { singleCompletion };
            }

            // Process all completions
            foreach (var completion in completions)
            {
                await ProcessSingleCompletionAsync(completion, commandExecutionService, routineCommandRepository);
            }
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

    private async Task ProcessSingleCompletionAsync(
        RoutineCompletionDto completion,
        ICommandExecutionService commandExecutionService,
        IRoutineCommandRepository routineCommandRepository)
    {
        // Guard clause: Validate commandId is not null or empty to prevent System.ArgumentNullException
        if (string.IsNullOrEmpty(completion.CommandId))
        {
            _logger.LogError("❌ Completion received with NULL or empty commandId. Ignoring to prevent System.ArgumentNullException. ESP32 ID: {Esp32Id}, Status: {Status}",
                completion.Esp32Id ?? "unknown", completion.Status ?? "unknown");
            return; // Exit method without processing the invalid completion
        }

        _logger.LogDebug("📦 Processing completion: CommandId={CommandId}, Status={Status}",
            completion.CommandId, completion.Status);

        // 1. Process command completion in memory (releases pin, updates state, activates pending)
        try
        {
            await commandExecutionService.OnCommandCompletedAsync(completion.CommandId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogDebug("ℹ️ Command {CommandId} not found in active commands (may be a power OFF command or already processed)",
                completion.CommandId);
        }

        // 2. Update database record to FINISHED (if it exists)
        var routineCommand = await routineCommandRepository.GetByCommandIdAsync(completion.CommandId);
        if (routineCommand != null)
        {
            // Update status and timestamps
            routineCommand.StatusGeneral = RoutineCommandStatus.FINISHED;
            routineCommand.FinishedAt = DateTime.UtcNow;

            // Calculate total duration in seconds
            var totalDuration = (routineCommand.FinishedAt.Value - routineCommand.CreatedAt).TotalSeconds;
            routineCommand.TotalDurationSeconds = totalDuration;

            await routineCommandRepository.UpdateAsync(routineCommand);

            _logger.LogInformation("✅ Marked command {CommandId} as FINISHED at {FinishedAt} - Total duration: {Duration}s - Firmware status: {FirmwareStatus}",
                completion.CommandId, routineCommand.FinishedAt, totalDuration, completion.Status);
        }
        else
        {
            _logger.LogDebug("ℹ️ Command {CommandId} not found in database (likely a power OFF command)",
                completion.CommandId);
        }
    }
}