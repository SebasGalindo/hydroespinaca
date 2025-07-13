using SensorService.Domain.Exceptions;
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
        try
        {
            await _collection.InsertOneAsync(AggregateMapper.ToDocument(aggregate));
        }
        catch (Exception ex)
        {
            // Log or handle the exception
            throw new DatabaseOperationException("Error creating aggregate", ex);
        }
    }

    public async Task<List<Aggregate>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        try
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
        catch (Exception ex)
        {
            // Log or handle the exception
            throw new DatabaseOperationException("Error retrieving aggregates", ex);
        }
    }
    public async Task<Aggregate?> GetBySensorAndVariableAndTimestampAsync(string sensorId, string variableId, DateTime timestamp)
    {
        var filter = Builders<AggregateDocument>.Filter.And(
            Builders<AggregateDocument>.Filter.Eq(x => x.SensorId, sensorId),
            Builders<AggregateDocument>.Filter.Eq(x => x.VariableId, variableId),
            Builders<AggregateDocument>.Filter.Eq(x => x.Timestamp, timestamp)
        );

        var doc = await _collection.Find(filter).FirstOrDefaultAsync();
        return doc is null ? null : AggregateMapper.ToEntity(doc);
    }

}
