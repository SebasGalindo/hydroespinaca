using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Esp32Node;
using SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Application.UseCases.Esp32OfflineWorker;

public class CheckEsp32OfflineStatusUseCase : ICheckEsp32OfflineStatusUseCase
{
    private readonly IEsp32StatusService _esp32StatusService;
    private readonly IEsp32OfflineNotificationService _notificationService;
    private readonly IEsp32AlertRepository _alertRepository;
    private readonly ILogger<CheckEsp32OfflineStatusUseCase> _logger;

    public CheckEsp32OfflineStatusUseCase(
        IEsp32StatusService esp32StatusService,
        IEsp32OfflineNotificationService notificationService,
        IEsp32AlertRepository alertRepository,
        ILogger<CheckEsp32OfflineStatusUseCase> logger)
    {
        _esp32StatusService = esp32StatusService;
        _notificationService = notificationService;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<CheckEsp32StatusResult> ExecuteAsync(
     OfflineThreshold threshold,
     DateTime? currentTime = null)
    {
        var checkTime = currentTime ?? DateTime.UtcNow;
        var statuses = await _esp32StatusService.GetAllEsp32StatusesAsync(checkTime, threshold);

        var statusList = statuses.ToList();
        var offlineStatuses = statusList.Where(s => s.IsOffline).ToList();
        var onlineStatuses = statusList.Where(s => !s.IsOffline).ToList();

        var alertsCreated = 0;
        var alertsResolved = 0;

        // Process offline ESP32s
        foreach (var offlineStatus in offlineStatuses)
        {
            _logger.LogWarning(
                "🚨 ESP32 {Esp32Id} desconectado. Última actividad: {LastActivity:u}",
                offlineStatus.Esp32Id, offlineStatus.LastActivity);

            // Create or update offline alert
            await _esp32StatusService.UpsertOfflineAlertAsync(offlineStatus, checkTime);

            // Check if we should send email
            var shouldSend = await _notificationService.ShouldSendAlertAsync(offlineStatus.Esp32Id.Value);

            if (shouldSend)
            {
                var alert = await _alertRepository.GetActiveByEsp32IdAsync(offlineStatus.Esp32Id.Value);
                if (alert != null && alert.EmailSentAt == null)
                {
                    await _notificationService.SendOfflineAlertAsync(alert);
                    await _notificationService.MarkAlertAsSentAsync(alert.Id);
                    _logger.LogInformation("📧 Sent offline notification for ESP32: {Esp32Id}", offlineStatus.Esp32Id.Value);
                }
            }

            alertsCreated++;
        }

        // Process online ESP32s - resolve any active alerts
        foreach (var onlineStatus in onlineStatuses)
        {
            await _esp32StatusService.ResolveOfflineAlertAsync(onlineStatus.Esp32Id.Value, checkTime);
            alertsResolved++;
        }

        var result = new CheckEsp32StatusResult(
            statusList.Count,
            offlineStatuses.Count,
            alertsCreated);

        if (offlineStatuses.Any())
        {
            _logger.LogInformation(
                "✅ ESP32 status check completed. {TotalCount} checked, {OfflineCount} offline, {OnlineCount} online",
                result.TotalEsp32Checked, result.OfflineEsp32Count, onlineStatuses.Count);
        }

        return result;
    }
}
