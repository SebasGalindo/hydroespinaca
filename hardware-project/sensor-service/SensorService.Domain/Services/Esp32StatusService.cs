using HydroEspinaca.Shared.Utils;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

public class Esp32StatusService : IEsp32StatusService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IEsp32NodeRepository _esp32NodeRepository;
    private readonly IReadingRepository _readingRepository;
    private readonly IEsp32AlertRepository _esp32AlertRepository;
    private readonly ILogger<Esp32StatusService> _logger;
    public Esp32StatusService(
        ISensorRepository sensorRepository,
        IReadingRepository readingRepository,
        IEsp32AlertRepository esp32AlertRepository,
        IEsp32NodeRepository esp32NodeRepository,
        ILogger<Esp32StatusService> logger)
    {
        _sensorRepository = sensorRepository;
        _readingRepository = readingRepository;
        _esp32AlertRepository = esp32AlertRepository;
        _esp32NodeRepository = esp32NodeRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Esp32StatusRecord>> GetAllEsp32StatusesAsync(
     DateTime currentTime,
     OfflineThreshold threshold)
    {
        var allSensors = await _sensorRepository.GetAllAsync();
        var allEsp32Nodes = await _esp32NodeRepository.GetAllAsync();

        var esp32Groups = allSensors
            .Where(s => !string.IsNullOrWhiteSpace(s.Esp32Id))
            .GroupBy(s => s.Esp32Id!)
            .ToList();

        var statuses = new List<Esp32StatusRecord>();

        foreach (var group in esp32Groups)
        {
            var esp32IdStr = group.Key;
            var esp32Id = Esp32Id.Create(esp32IdStr);
            var sensorCodes = group.Select(s => s.Code).ToList();

            var esp32Node = allEsp32Nodes.FirstOrDefault(n => n.Id == esp32IdStr);
            if (esp32Node is null)
            {
                _logger.LogWarning("ESP32 node with ID '{Esp32Id}' not found.", esp32IdStr);
                continue;
            }

            var lastReading = await _readingRepository.GetLatestBySensorCodesAsync(sensorCodes);
            var lastSensorTime = lastReading?.Timestamp ?? DateTime.MinValue;
            var lastSeenTime = esp32Node.LastSeen;


            var lastActivity = (lastSensorTime > lastSeenTime) ? lastSensorTime : lastSeenTime;

            var statusRecord = Esp32StatusRecord.Create(esp32Id, lastActivity, currentTime, threshold);
            statuses.Add(statusRecord);
        }

        return statuses;
    }


    public async Task UpsertOfflineAlertAsync(
        Esp32StatusRecord status,
        DateTime timestamp)
    {
        var existingAlert = await _esp32AlertRepository.GetActiveByEsp32IdAsync(status.Esp32Id.Value);

        var readableDuration = TimeFormatter.FormatInactivity(status.TimeSinceLastActivity);
        var message = $"El dispositivo {status.Esp32Id.Value} se ha desconectado del sistema. No envía datos desde hace {readableDuration}.";

        if (existingAlert is not null)
        {
            // Update existing alert with latest disconnect info
            existingAlert.Message = message;
            existingAlert.Timestamp = timestamp;
            await _esp32AlertRepository.UpdateAsync(existingAlert);

            _logger.LogDebug("Updated existing offline alert for ESP32: {Esp32Id}", status.Esp32Id.Value);
        }
        else
        {
            // Create new offline alert
            var newAlert = new Esp32Alert
            {
                Esp32Id = status.Esp32Id.Value,
                Timestamp = timestamp,
                Message = message,
                Acknowledged = false
            };

            await _esp32AlertRepository.CreateAsync(newAlert);

            _logger.LogWarning("Created offline alert for ESP32: {Esp32Id}", status.Esp32Id.Value);
        }
    }

    public async Task ResolveOfflineAlertAsync(string esp32Id, DateTime timestamp)
    {
        var activeAlert = await _esp32AlertRepository.GetActiveByEsp32IdAsync(esp32Id);

        if (activeAlert is null)
            return;

        activeAlert.ResolvedAt = timestamp;
        activeAlert.Message = $"El dispositivo {esp32Id} se ha reconectado exitosamente.";
        await _esp32AlertRepository.UpdateAsync(activeAlert);

        _logger.LogInformation("Resolved offline alert for ESP32: {Esp32Id}", esp32Id);
    }

    public async Task AcknowledgeOfflineAlertAsync(string esp32Id)
    {
        var alert = await _esp32AlertRepository.GetActiveByEsp32IdAsync(esp32Id);

        if (alert is null)
            return;

        alert.Acknowledged = true;
        alert.Timestamp = DateTime.UtcNow;

        await _esp32AlertRepository.UpdateAsync(alert);
    }
}
