using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.ValueObjects;

namespace SensorService.Domain.Services;

public class Esp32StatusService : IEsp32StatusService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IReadingRepository _readingRepository;

    public Esp32StatusService(
        ISensorRepository sensorRepository,
        IReadingRepository readingRepository)
    {
        _sensorRepository = sensorRepository;
        _readingRepository = readingRepository;
    }

    public async Task<IEnumerable<Esp32Status>> GetAllEsp32StatusesAsync(
        DateTime currentTime,
        OfflineThreshold threshold)
    {
        var allSensors = await _sensorRepository.GetAllAsync();

        var esp32Groups = allSensors
            .Where(s => !string.IsNullOrEmpty(s.Esp32Id))
            .GroupBy(s => s.Esp32Id!)
            .ToList();

        var statuses = new List<Esp32Status>();

        foreach (var group in esp32Groups)
        {
            var esp32Id = Esp32Id.Create(group.Key);
            var sensorIds = group.Select(s => s.Id!).ToList();

            var lastReading = await _readingRepository.GetLatestBySensorIdsAsync(sensorIds);
            var lastActivity = lastReading?.Timestamp ?? DateTime.MinValue;

            var status = Esp32Status.Create(esp32Id, lastActivity, currentTime, threshold);
            statuses.Add(status);
        }

        return statuses;
    }

    public SensorAlert CreateOfflineAlert(
        Esp32Status status,
        string sensorId,
        DateTime timestamp)
    {
        return new SensorAlert
        {
            SensorId = sensorId,
            Type = "Esp32Offline",
            Value = 0,
            Threshold = 0,
            Timestamp = timestamp,
            Severity = "critical",
            Message = $"ESP32 '{status.Esp32Id}' no ha enviado datos en más de {status.TimeSinceLastActivity.TotalMinutes:F1} minutos.",
            Acknowledged = false
        };
    }
}