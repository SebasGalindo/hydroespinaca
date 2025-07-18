using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Services;

public class Esp32OfflineWorker : BackgroundService
{
    private readonly ILogger<Esp32OfflineWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(2);

    public Esp32OfflineWorker(
        ILogger<Esp32OfflineWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sensorRepo = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
                var readingRepo = scope.ServiceProvider.GetRequiredService<IReadingRepository>();
                var alertRepo = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();

                await CheckEsp32StatusAsync(sensorRepo, readingRepo, alertRepo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking ESP32 offline status.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckEsp32StatusAsync(
        ISensorRepository sensorRepo,
        IReadingRepository readingRepo,
        ISensorAlertRepository alertRepo)
    {
        var now = DateTime.UtcNow;

        var allSensors = await sensorRepo.GetAllAsync();
        var esp32Groups = allSensors
            .Where(s => !string.IsNullOrEmpty(s.Esp32Id))
            .GroupBy(s => s.Esp32Id)
            .ToList();

        foreach (var group in esp32Groups)
        {
            var esp32Id = group.Key!;
            var sensorIds = group.Select(s => s.Id!).ToList();

            var lastReading = await readingRepo.GetLatestBySensorIdsAsync(sensorIds);
            var latestTimestamp = lastReading?.Timestamp ?? DateTime.MinValue;

            if (now - latestTimestamp > _offlineThreshold)
            {
                _logger.LogWarning($"🚨 ESP32 {esp32Id} parece estar desconectado. Última lectura: {latestTimestamp:u}");

                var anySensorId = group.First().Id!;
                await alertRepo.CreateAsync(new SensorAlert
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
