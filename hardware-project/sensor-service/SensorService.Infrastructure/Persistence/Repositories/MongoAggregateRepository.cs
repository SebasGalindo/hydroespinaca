using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Exceptions;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoAggregateRepository : IAggregateRepository
{
    private readonly BaseMongoRepository<Aggregate, AggregateDocument> _baseRepo;

    public MongoAggregateRepository(IConfiguration config, IEntityMapper<Aggregate, AggregateDocument> mapper)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);

        _baseRepo = new BaseMongoRepository<Aggregate, AggregateDocument>(
            db, "aggregates", mapper
        );
    }

    public async Task CreateAsync(Aggregate aggregate)
    {
        try
        {
            await _baseRepo.CreateAsync(aggregate);
        }
        catch (Exception ex)
        {
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

            return await _baseRepo.FindManyAsync(filter);
        }
        catch (Exception ex)
        {
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

        return await _baseRepo.FindOneAsync(filter);
    }
}
