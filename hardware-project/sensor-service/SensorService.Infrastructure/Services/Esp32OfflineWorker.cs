using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Application.UseCases.Esp32OfflineWorker;
using SensorService.Domain.ValueObjects;

namespace SensorService.Infrastructure.Services;

public class Esp32OfflineWorker : BackgroundService
{
    private readonly ILogger<Esp32OfflineWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly OfflineThreshold _offlineThreshold = OfflineThreshold.FromMinutes(2);

    public Esp32OfflineWorker(
        ILogger<Esp32OfflineWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Esp32OfflineWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteStatusCheckCycleAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking ESP32 offline status");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("🛑 Esp32OfflineWorker stopped");
    }

    private async Task ExecuteStatusCheckCycleAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<CheckEsp32OfflineStatusUseCase>();

        var result = await useCase.ExecuteAsync(_offlineThreshold);

        _logger.LogDebug(
            "ESP32 status check cycle completed: {TotalCount} checked, {OfflineCount} offline, {AlertsCount} alerts created",
            result.TotalEsp32Checked, result.OfflineEsp32Count, result.AlertsCreated);
    }
}