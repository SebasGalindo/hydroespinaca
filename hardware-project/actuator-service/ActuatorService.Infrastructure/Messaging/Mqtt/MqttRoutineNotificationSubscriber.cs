using ActuatorService.Application.Interfaces;
using ActuatorService.Domain.Interfaces;
using HydroEspinaca.Shared.Constants;
using HydroEspinaca.Shared.DTOs.Actuator;
using HydroEspinaca.Shared.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActuatorService.Infrastructure.Messaging.Mqtt;

public class MqttRoutineNotificationSubscriber : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMqttClientService _mqttClient;
    private readonly ILogger<MqttRoutineNotificationSubscriber> _logger;
    private const string NOTIFICATION_TOPIC = MqttTopics.Actuator.RoutineNotifications;

    public MqttRoutineNotificationSubscriber(
        IServiceProvider serviceProvider,
        IMqttClientService mqttClient,
        ILogger<MqttRoutineNotificationSubscriber> logger)
    {
        _serviceProvider = serviceProvider;
        _mqttClient = mqttClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Starting MQTT Routine Notification Subscriber...");

        try
        {
            await _mqttClient.SubscribeAsync(NOTIFICATION_TOPIC, async (topic, payload) =>
            {
                await OnRoutineNotificationReceived(topic, payload, stoppingToken);
            });

            _logger.LogInformation("✅ MQTT Routine Notification Subscriber started and subscribed to topic: {Topic}", NOTIFICATION_TOPIC);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("📄 MQTT Routine Notification Subscriber is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in MQTT Routine Notification Subscriber");
            throw;
        }
    }

    private async Task OnRoutineNotificationReceived(string topic, string payload, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        // Only process messages from the notification topic
        if (topic != NOTIFICATION_TOPIC)
        {
            _logger.LogDebug("🚫 Ignoring message from topic: {Topic} (expected: {ExpectedTopic})", topic, NOTIFICATION_TOPIC);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var jobScheduleStateManager = scope.ServiceProvider.GetRequiredService<IJobScheduleStateManager>();

        try
        {
            _logger.LogDebug("📨 Received routine notification on topic: {Topic}", topic);

            var notification = JsonSerializer.Deserialize<RoutineNotificationDto>(payload, JsonConstants.SerializerOptions.CamelCase);

            if (notification == null)
            {
                _logger.LogWarning("⚠️ Received null notification data from topic: {Topic}", topic);
                return;
            }

            _logger.LogInformation("📬 Processing notification: {Decision} for channel {ChannelId}, affected: {AffectedCommand}, target: {TargetCommand}", 
                notification.Decision, notification.ChannelId, notification.AffectedCommand, notification.TargetCommand);

            switch (notification.Decision.ToLowerInvariant())
            {
                case var decision when decision == ActuatorConstants.FirmwareDecisions.Consolidated.ToLowerInvariant():
                    await HandleConsolidatedNotification(jobScheduleStateManager, notification);
                    break;
                case var decision when decision == ActuatorConstants.FirmwareDecisions.Queued.ToLowerInvariant():
                    await HandleQueuedNotification(jobScheduleStateManager, notification);
                    break;
                default:
                    _logger.LogWarning("⚠️ Unknown notification decision: {Decision}. Valid options: {ValidDecisions}", 
                        notification.Decision, string.Join(", ", ActuatorConstants.FirmwareDecisions.ValidDecisions));
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to deserialize routine notification from topic: {Topic}, Payload: {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing routine notification from topic: {Topic}", topic);
        }
    }

    private async Task HandleConsolidatedNotification(IJobScheduleStateManager stateManager, RoutineNotificationDto notification)
    {
        using var scope = _serviceProvider.CreateScope();
        var routineCommandRepository = scope.ServiceProvider.GetRequiredService<IRoutineCommandRepository>();
        var stateMachine = scope.ServiceProvider.GetRequiredService<IActuatorStateMachine>();

        _logger.LogInformation("🔄 Handling consolidated notification: removing {AffectedCommand}, updating {TargetCommand}",
            notification.AffectedCommand, notification.TargetCommand);

        // Remove the affected command from in-memory queue
        stateManager.RemoveCompletedRoutine(notification.AffectedCommand);

        // Remove the affected command from database
        var affectedCommand = await routineCommandRepository.GetByCommandIdAsync(notification.AffectedCommand);
        if (affectedCommand != null)
        {
            await routineCommandRepository.DeleteAsync(affectedCommand.Id);
            _logger.LogInformation("🗑️ Deleted consolidated routine command {CommandId} from database", notification.AffectedCommand);
        }

        // Update the target command status
        stateManager.UpdateCommandStatus(notification.TargetCommand, ActuatorConstants.CommandStatuses.Running);

        // Note: Actuator state remains ON (no state change needed for consolidation)
        _logger.LogDebug("📊 Actuator state remains ON (consolidation does not change state)");

        _logger.LogInformation("✅ Consolidated notification processed successfully");
    }

    private async Task HandleQueuedNotification(IJobScheduleStateManager stateManager, RoutineNotificationDto notification)
    {
        _logger.LogInformation("📋 Handling queued notification: maintaining order for {TargetCommand}", notification.TargetCommand);
        
        // For queued decisions, we maintain the current job schedule order
        // The firmware has decided to queue the command, so we don't change anything
        _logger.LogInformation("✅ Queued notification processed successfully - no changes to job schedule");
        await Task.CompletedTask;
    }
}