using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Services;

/// <summary>
/// Background service that initializes the database with seed data on application startup
/// </summary>
public class DatabaseInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Starting database initialization...");

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // Seed variables first (required for sensors)
            var variableSeedService = scope.ServiceProvider.GetRequiredService<IVariableSeedService>();
            var variablesSeeded = await variableSeedService.SeedDefaultVariablesAsync();

            if (variablesSeeded > 0)
            {
                _logger.LogInformation("✅ Variables initialized: {Count} variables seeded", variablesSeeded);
            }

            // Seed sensors (depends on variables and ESP32 nodes)
            var sensorSeedService = scope.ServiceProvider.GetRequiredService<ISensorSeedService>();
            var sensorsSeeded = await sensorSeedService.SeedDefaultSensorsAsync();

            if (sensorsSeeded > 0)
            {
                _logger.LogInformation("✅ Sensors initialized: {Count} sensors seeded", sensorsSeeded);
            }

            _logger.LogInformation("✅ Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during database initialization");
            // Don't throw - allow the application to start even if seeding fails
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database initialization service stopped");
        return Task.CompletedTask;
    }
}
