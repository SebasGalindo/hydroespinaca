using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.Esp32OfflineWorker;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;
using SensorService.Application.DTOs.Esp32Node;

namespace SensorService.Application.UseCases.Esp32OfflineWorker;

public class CheckEsp32OfflineStatusUseCase : ICheckEsp32OfflineStatusUseCase
{
    private readonly IEsp32StatusService _esp32StatusService;
    private readonly ISensorRepository _sensorRepository;
    private readonly ISensorAlertRepository _alertRepository;
    private readonly ILogger<CheckEsp32OfflineStatusUseCase> _logger;

    public CheckEsp32OfflineStatusUseCase(
        IEsp32StatusService esp32StatusService,
        ISensorRepository sensorRepository,
        ISensorAlertRepository alertRepository,
        ILogger<CheckEsp32OfflineStatusUseCase> logger)
    {
        _esp32StatusService = esp32StatusService;
        _sensorRepository = sensorRepository;
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

        var alertsCreated = 0;

        foreach (var offlineStatus in offlineStatuses)
        {
            _logger.LogWarning(
                "🚨 ESP32 {Esp32Id} parece estar desconectado. Última lectura: {LastActivity:u}",
                offlineStatus.Esp32Id, offlineStatus.LastActivity);

            var sensorId = await GetAnySensorIdForEsp32Async(offlineStatus.Esp32Id);
            if (sensorId != null)
            {
                var alert = _esp32StatusService.CreateOfflineAlert(offlineStatus, sensorId, checkTime);
                await _alertRepository.CreateAsync(alert);
                alertsCreated++;
            }
        }

        var result = new CheckEsp32StatusResult(
            statusList.Count,
            offlineStatuses.Count,
            alertsCreated);

        if (offlineStatuses.Any())
        {
            _logger.LogInformation(
                "✅ ESP32 status check completed. {TotalCount} checked, {OfflineCount} offline, {AlertsCount} alerts created",
                result.TotalEsp32Checked, result.OfflineEsp32Count, result.AlertsCreated);
        }

        return result;
    }

    private async Task<string?> GetAnySensorIdForEsp32Async(Esp32Id esp32Id)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        return allSensors.FirstOrDefault(s => s.Esp32Id == esp32Id.Value)?.Id;
    }
}