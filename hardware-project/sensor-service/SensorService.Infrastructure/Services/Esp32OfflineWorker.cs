using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Entities;

namespace SensorService.Infrastructure.Services;

public class Esp32OfflineWorker : BackgroundService
{
    private readonly ILogger<Esp32OfflineWorker> _logger;
    private readonly ISensorRepository _sensorRepo;
    private readonly IReadingRepository _readingRepo;
    private readonly ISensorAlertRepository _alertRepo;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(2);

    public Esp32OfflineWorker(
        ILogger<Esp32OfflineWorker> logger,
        ISensorRepository sensorRepo,
        IReadingRepository readingRepo,
        ISensorAlertRepository alertRepo)
    {
        _logger = logger;
        _sensorRepo = sensorRepo;
        _readingRepo = readingRepo;
        _alertRepo = alertRepo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckEsp32StatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ESP32 offline status.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckEsp32StatusAsync()
    {
        var now = DateTime.UtcNow;

        var allSensors = await _sensorRepo.GetAllAsync();
        var esp32Groups = allSensors
            .Where(s => !string.IsNullOrEmpty(s.Esp32Id))
            .GroupBy(s => s.Esp32Id)
            .ToList();

        foreach (var group in esp32Groups)
        {
            var esp32Id = group.Key!;
            var sensorIds = group.Select(s => s.Id!).ToList();

            var lastReading = await _readingRepo.GetLatestBySensorIdsAsync(sensorIds);

            var latestTimestamp = lastReading?.Timestamp ?? DateTime.MinValue;

            if (now - latestTimestamp > _offlineThreshold)
            {
                // Ya está desconectado
                _logger.LogWarning($"🚨 ESP32 {esp32Id} parece estar desconectado. Última lectura: {latestTimestamp:u}");

                var anySensorId = group.First().Id!;


                await _alertRepo.CreateAsync(new SensorAlert
                {
                    SensorId = anySensorId,
                    Type = "Esp32Offline",
                    Value = 0,
                    Threshold = 0,
                    Timestamp = now,
                    Severity = "critical",
                    Message = $"ESP32 '{esp32Id}' no ha enviado datos en más de {_offlineThreshold.TotalMinutes} minutos.",
                    Acknowledged = false
                });
            }
        }
    }
}
