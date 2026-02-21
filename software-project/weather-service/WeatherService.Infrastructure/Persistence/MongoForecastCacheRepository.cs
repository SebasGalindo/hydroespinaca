using MongoDB.Driver;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;
using WeatherService.Domain.Persistence.Documents;
using WeatherService.Infrastructure.Persistence.Mappers;

namespace WeatherService.Infrastructure.Persistence;

/// <summary>
/// Repository for managing forecast cache in MongoDB. 
/// This repository provides methods to retrieve the latest forecast cache and 
/// to upsert (insert or update) the forecast cache document. 
/// The implementation uses a singleton pattern where only one document 
/// with a fixed ID ("latest_forecast") is stored in the collection, 
/// representing the most recent forecast data. 
/// The repository interacts with the MongoDB collection using the ForecastCacheMapper 
/// to convert between domain entities and MongoDB documents.
public class MongoForecastCacheRepository : IForecastCacheRepository
{
    private readonly IMongoCollection<ForecastCacheDocument> _collection;
    private readonly ForecastCacheMapper _mapper = new();

    private const string SingletonId = "latest_forecast";

    public MongoForecastCacheRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ForecastCacheDocument>("weather_forecast_cache");
    }

    /// <inheritdoc/>
    public async Task<ForecastCache?> GetLatestAsync(CancellationToken ct = default)
    {
        var filter = Builders<ForecastCacheDocument>.Filter.Eq(x => x.Id, SingletonId);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : _mapper.ToEntity(doc);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(ForecastCache cache, CancellationToken ct = default)
    {
        cache.Id = SingletonId;
        var doc = _mapper.ToDocument(cache);

        var filter = Builders<ForecastCacheDocument>.Filter.Eq(x => x.Id, SingletonId);
        await _collection.ReplaceOneAsync(
            filter, doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }
}
