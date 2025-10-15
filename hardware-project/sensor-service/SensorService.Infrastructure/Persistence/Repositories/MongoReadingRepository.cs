using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoReadingRepository : IReadingRepository
{
    private readonly BaseMongoRepository<Reading, ReadingDocument> _baseRepo;

    public MongoReadingRepository(MongoDbContext ctx, IEntityMapper<Reading, ReadingDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Reading, ReadingDocument>(ctx.Database, "readings", mapper);
    }

    public async Task CreateAsync(Reading reading)
    {
        await _baseRepo.CreateAsync(reading);
    }

    public async Task<List<Reading>> GetBySensorAndVariableAsync(string sensorCode, string variableCode, DateTime from, DateTime to)
    {
        var filter = Builders<ReadingDocument>.Filter.And(
            Builders<ReadingDocument>.Filter.Eq(x => x.SensorCode, sensorCode),
            Builders<ReadingDocument>.Filter.Eq(x => x.VariableCode, variableCode),
            Builders<ReadingDocument>.Filter.Gte(x => x.Timestamp, from),
            Builders<ReadingDocument>.Filter.Lte(x => x.Timestamp, to)
        );

        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        var filter = Builders<ReadingDocument>.Filter.Lt(x => x.Timestamp, cutoff);
        var result = await _baseRepo.DeleteManyAsync(filter);
        return (int)result.DeletedCount;
    }

    public async Task<Reading?> GetLatestBySensorCodesAsync(List<string> sensorCodes)
    {
        var filter = Builders<ReadingDocument>.Filter.In(r => r.SensorCode, sensorCodes);
        var sort = Builders<ReadingDocument>.Sort.Descending(r => r.Timestamp);
        return await _baseRepo.FindLastOneAsync(filter, sort);
    }

    public async Task<List<Reading>> GetLatestReadingsByVariableAsync()
    {
        // Aggregate pipeline to get the latest reading for each variableCode
        var pipeline = new[]
        {
            // Sort by timestamp descending to get latest first
            new BsonDocument("$sort", new BsonDocument("timestamp", -1)),

            // Group by variableCode and take the first (latest) document
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$variableCode" },
                { "latestReading", new BsonDocument("$first", "$$ROOT") }
            }),

            // Replace root with the latest reading document
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latestReading")),

            // Sort by variableCode for consistent ordering
            new BsonDocument("$sort", new BsonDocument("variableCode", 1))
        };

        return await _baseRepo.AggregateAsync(pipeline);
    }
}
