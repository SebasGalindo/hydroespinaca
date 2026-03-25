using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;
using WeatherService.Domain.Persistence.Documents;
using WeatherService.Infrastructure.Persistence.Mappers;

namespace WeatherService.Infrastructure.Persistence;

/// <summary>
/// Repository for managing weather alert configurations in MongoDB. 
/// This repository provides methods to retrieve, create, update, 
/// and check the existence of weather alert configurations based on a 
/// fuzzy system ID. The implementation uses a base repository for common CRUD 
/// operations and directly interacts with the MongoDB collection for specific queries.
/// A unique index is created on the fuzzy_system_id field to ensure that each 
/// fuzzy system has only one associated alert configuration. 
/// </summary>
public class MongoWeatherAlertConfigRepository : IWeatherAlertConfigRepository
{
    private readonly BaseMongoRepository<WeatherAlertConfig, WeatherAlertConfigDocument> _base;
    private readonly IMongoCollection<WeatherAlertConfigDocument> _collection;

    public MongoWeatherAlertConfigRepository(IMongoDatabase database)
    {
        _base = new BaseMongoRepository<WeatherAlertConfig, WeatherAlertConfigDocument>(
            database, "weather_alert_configs", new WeatherAlertConfigMapper());

        _collection = database.GetCollection<WeatherAlertConfigDocument>("weather_alert_configs");

        // Unique index on fuzzy_system_id
        var indexKeys = Builders<WeatherAlertConfigDocument>.IndexKeys
            .Ascending(x => x.FuzzySystemId);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<WeatherAlertConfigDocument>(indexKeys, indexOptions));
    }

    /// <inheritdoc/>
    public async Task<WeatherAlertConfig?> GetByFuzzySystemIdAsync(string fuzzySystemId, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertConfigDocument>.Filter.Eq(x => x.FuzzySystemId, fuzzySystemId);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        if (doc == null) return null;
        return new WeatherAlertConfigMapper().ToEntity(doc);
    }

    /// <inheritdoc/>
    public async Task<List<WeatherAlertConfig>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertConfigDocument>.Filter.Eq(x => x.IsActive, true);
        var docs = await _collection.Find(filter).ToListAsync(ct);
        var mapper = new WeatherAlertConfigMapper();
        return docs.Select(mapper.ToEntity).ToList();
    }

    /// <inheritdoc/>
    public async Task<WeatherAlertConfig> CreateAsync(WeatherAlertConfig config, CancellationToken ct = default)
    {
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        await _base.CreateAsync(config);
        return config;
    }

    /// <inheritdoc/>
    public async Task<WeatherAlertConfig?> UpdateAsync(string fuzzySystemId, WeatherAlertConfig config, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertConfigDocument>.Filter.Eq(x => x.FuzzySystemId, fuzzySystemId);
        var mapper = new WeatherAlertConfigMapper();
        var doc = mapper.ToDocument(config);

        var result = await _collection.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        return result.MatchedCount > 0 ? config : null;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string fuzzySystemId, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertConfigDocument>.Filter.Eq(x => x.FuzzySystemId, fuzzySystemId);
        var result = await _collection.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string fuzzySystemId, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertConfigDocument>.Filter.Eq(x => x.FuzzySystemId, fuzzySystemId);
        return await _collection.Find(filter).Limit(1).AnyAsync(ct);
    }
}
