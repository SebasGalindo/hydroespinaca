using ActuatorService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Services;

public class DatabaseCleanupService : BackgroundService
{
    private readonly ILogger<DatabaseCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(7); // Weekly cleanup
    private readonly TimeSpan _recordRetentionPeriod = TimeSpan.FromDays(30); // Keep records for 30 days

    public DatabaseCleanupService(
        ILogger<DatabaseCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Database cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database cleanup");
                // Wait a shorter period before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var cutoffDate = DateTime.UtcNow.Subtract(_recordRetentionPeriod);

        _logger.LogInformation("Starting database cleanup for records older than {CutoffDate}", cutoffDate);

        await CleanupRoutineCommandsAsync(scope, cutoffDate);

        _logger.LogInformation("Database cleanup completed successfully");
    }

    private async Task CleanupRoutineCommandsAsync(IServiceScope scope, DateTime cutoffDate)
    {
        try
        {
            var routineCommandRepository = scope.ServiceProvider.GetRequiredService<IRoutineCommandRepository>();
            
            _logger.LogInformation("Cleaning up routine commands (actuator commands) older than {CutoffDate}", cutoffDate);
            
            var deletedCount = await routineCommandRepository.DeleteOlderThanAsync(cutoffDate);
            _logger.LogInformation("Deleted {Count} routine commands", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup routine commands");
        }
    }
}