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
        var doc = ReadingMapper.ToDocument(reading);
        await _collection.InsertOneAsync(doc);
    }

    public async Task<List<Reading>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
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

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        var filter = Builders<ReadingDocument>.Filter.Lt(x => x.Timestamp, cutoff);
        await _collection.DeleteManyAsync(filter);
    }
}
