using HydroEspinaca.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Services;

/// <summary>
/// Background worker that periodically cleans up resolved or expired alerts from the database.
/// </summary>
public class AlertCleanupWorker : BackgroundService
{
    private readonly ILogger<AlertCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(SensorConstants.Cleanup.CleanupIntervalDays);
    private readonly TimeSpan _alertRetentionPeriod = TimeSpan.FromDays(SensorConstants.Cleanup.AlertRetentionDays);

    public AlertCleanupWorker(
        ILogger<AlertCleanupWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🧹 Alert cleanup worker started - Interval: {CleanupInterval}, Retention: {AlertRetention}", 
            _cleanupInterval, _alertRetentionPeriod);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Alert cleanup worker is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ An error occurred during alert cleanup");
                // Wait a shorter period before retrying on error
                await Task.Delay(TimeSpan.FromHours(SensorConstants.Cleanup.ErrorRetryDelayHours), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var cutoffDate = DateTime.UtcNow.Subtract(_alertRetentionPeriod);

        _logger.LogInformation("🧹 Starting alert cleanup for records older than {CutoffDate}", cutoffDate);

        var totalDeleted = 0;
        totalDeleted += await CleanupEsp32AlertsAsync(scope, cutoffDate);
        totalDeleted += await CleanupSensorAlertsAsync(scope, cutoffDate);

        if (totalDeleted > 0)
        {
            _logger.LogInformation("✅ Alert cleanup completed successfully - Deleted {TotalDeleted} records", totalDeleted);
        }
        else
        {
            _logger.LogDebug("ℹ️ Alert cleanup completed - No old records found to delete");
        }
    }

    private async Task<int> CleanupEsp32AlertsAsync(IServiceScope scope, DateTime cutoffDate)
    {
        try
        {
            var esp32AlertRepository = scope.ServiceProvider.GetRequiredService<IEsp32AlertRepository>();
            
            _logger.LogDebug("🧹 Cleaning up ESP32 alerts older than {CutoffDate}", cutoffDate);
            
            var deletedCount = await esp32AlertRepository.DeleteOlderThanAsync(cutoffDate);
            
            if (deletedCount > 0)
            {
                _logger.LogInformation("🗑️ Deleted {Count} old ESP32 alerts", deletedCount);
            }
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to cleanup ESP32 alerts");
            return 0;
        }
    }

    private async Task<int> CleanupSensorAlertsAsync(IServiceScope scope, DateTime cutoffDate)
    {
        try
        {
            var sensorAlertRepository = scope.ServiceProvider.GetRequiredService<ISensorAlertRepository>();
            
            _logger.LogDebug("🧹 Cleaning up sensor alerts older than {CutoffDate}", cutoffDate);
            
            var deletedCount = await sensorAlertRepository.DeleteOlderThanAsync(cutoffDate);
            
            if (deletedCount > 0)
            {
                _logger.LogInformation("🗑️ Deleted {Count} old sensor alerts", deletedCount);
            }
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to cleanup sensor alerts");
            return 0;
        }
    }
}