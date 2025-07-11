using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoAggregateRepository : IAggregateRepository
{
    private readonly IMongoCollection<AggregateDocument> _collection;

    public MongoAggregateRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<AggregateDocument>("aggregates");
    }

    public async Task CreateAsync(Aggregate aggregate)
    {
        await _collection.InsertOneAsync(AggregateMapper.ToDocument(aggregate));
    }

    public async Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        var filter = Builders<AggregateDocument>.Filter.And(
            Builders<AggregateDocument>.Filter.Eq(x => x.SensorId, sensorId),
            Builders<AggregateDocument>.Filter.Eq(x => x.VariableId, variableId),
            Builders<AggregateDocument>.Filter.Gte(x => x.Timestamp, from),
            Builders<AggregateDocument>.Filter.Lte(x => x.Timestamp, to)
        );

        var docs = await _collection.Find(filter).ToListAsync();
        return docs.Select(AggregateMapper.ToEntity).ToList();
    }
}
