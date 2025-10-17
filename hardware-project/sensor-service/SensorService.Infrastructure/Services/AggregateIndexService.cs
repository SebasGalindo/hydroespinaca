using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Services;

/// <summary>
/// Service to ensure indexes exist on the aggregates collection for optimal query performance
/// </summary>
public class AggregateIndexService
{
    private readonly IMongoCollection<AggregateDocument> _collection;
    private readonly ILogger<AggregateIndexService> _logger;

    public AggregateIndexService(IConfiguration config, ILogger<AggregateIndexService> logger)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<AggregateDocument>("aggregates");
        _logger = logger;
    }

    /// <summary>
    /// Creates compound index on timestamp + variableCode for efficient environmental aggregates queries
    /// </summary>
    public async Task EnsureIndexesAsync()
    {
        try
        {
            _logger.LogInformation("🔧 Ensuring indexes on aggregates collection...");

            // Create compound index: timestamp (ascending) + variableCode (ascending)
            // This index optimizes queries that filter by date range and group by variableCode
            var indexKeys = Builders<AggregateDocument>.IndexKeys
                .Ascending(a => a.Timestamp)
                .Ascending(a => a.VariableCode);

            var indexOptions = new CreateIndexOptions
            {
                Name = "idx_timestamp_variableCode",
                Background = true // Create index in background to avoid blocking
            };

            var indexModel = new CreateIndexModel<AggregateDocument>(indexKeys, indexOptions);

            var indexName = await _collection.Indexes.CreateOneAsync(indexModel);

            _logger.LogInformation("✅ Index created successfully: {IndexName}", indexName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            _logger.LogInformation("ℹ️ Index already exists with different options, skipping creation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating indexes on aggregates collection");
            throw;
        }
    }
}
