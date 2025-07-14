using SensorService.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;
public class MongoReadingRepository : IReadingRepository
{
    private readonly IMongoCollection<ReadingDocument> _collection;

    public MongoReadingRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<ReadingDocument>("readings");
    }

    public async Task CreateAsync(Reading reading)
    {
        try
        {
            var doc = ReadingMapper.ToDocument(reading);
            await _collection.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error creating reading", ex);
        }
    }

    public async Task<List<Reading>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        try
        {
            var filter = Builders<ReadingDocument>.Filter.And(
                Builders<ReadingDocument>.Filter.Eq(x => x.SensorId, sensorId),
                Builders<ReadingDocument>.Filter.Eq(x => x.VariableId, variableId),
                Builders<ReadingDocument>.Filter.Gte(x => x.Timestamp, from),
                Builders<ReadingDocument>.Filter.Lte(x => x.Timestamp, to)
            );

            var docs = await _collection.Find(filter).ToListAsync();
            return docs.Select(ReadingMapper.ToEntity).ToList();
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving readings", ex);
        }
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        try
        {
            var filter = Builders<ReadingDocument>.Filter.Lt(x => x.Timestamp, cutoff);
            var result = await _collection.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting old readings", ex);
        }
    }
    public async Task<Reading?> GetLatestBySensorIdsAsync(List<string> sensorIds)
    {
        var filter = Builders<ReadingDocument>.Filter.In(r => r.SensorId, sensorIds);
        return (await _collection
            .Find(filter)
            .SortByDescending(r => r.Timestamp)
            .Limit(1)
            .FirstOrDefaultAsync()) is { } doc
            ? ReadingMapper.ToEntity(doc)
            : null;
    }

}
