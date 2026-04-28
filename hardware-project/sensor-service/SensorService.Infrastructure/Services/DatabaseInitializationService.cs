using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SensorService.Domain.Interfaces;

namespace SensorService.Infrastructure.Services;

/// <summary>
/// Background service that initializes the database with seed data on application startup
/// </summary>
public class DatabaseInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Starting database initialization...");

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // Ensure time series collections exist before indexes are created
            var client = new MongoClient(_configuration["Mongo:ConnectionString"]);
            var db = client.GetDatabase(_configuration["Mongo:Database"]);

            await EnsureTimeSeriesCollectionAsync(
                db, "readings", "timestamp", "sensorCode",
                TimeSeriesGranularity.Seconds, cancellationToken);

            await EnsureTimeSeriesCollectionAsync(
                db, "aggregates", "timestamp", "variableCode",
                TimeSeriesGranularity.Hours, cancellationToken);

            // Ensure indexes are created for optimal query performance
            var aggregateIndexService = scope.ServiceProvider.GetRequiredService<AggregateIndexService>();
            await aggregateIndexService.EnsureIndexesAsync();

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

    private async Task EnsureTimeSeriesCollectionAsync(
        IMongoDatabase db,
        string collectionName,
        string timeField,
        string metaField,
        TimeSeriesGranularity granularity,
        CancellationToken cancellationToken)
    {
        var existingCollections = await (await db.ListCollectionNamesAsync(cancellationToken: cancellationToken))
            .ToListAsync(cancellationToken);

        if (existingCollections.Contains(collectionName))
        {
            _logger.LogDebug(
                "Collection '{Collection}' already exists — skipping time series creation",
                collectionName);
            return;
        }

        var options = new CreateCollectionOptions
        {
            TimeSeriesOptions = new TimeSeriesOptions(timeField, metaField, granularity)
        };

        await db.CreateCollectionAsync(collectionName, options, cancellationToken);
        _logger.LogInformation(
            "✅ Time series collection created: {Collection} (timeField={TimeField}, metaField={MetaField})",
            collectionName, timeField, metaField);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database initialization service stopped");
        return Task.CompletedTask;
    }
}
