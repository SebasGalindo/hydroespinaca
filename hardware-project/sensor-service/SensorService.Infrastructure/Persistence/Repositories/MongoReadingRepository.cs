using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Exceptions;
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
}
