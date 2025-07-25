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
        try
        {
            await _baseRepo.CreateAsync(reading);
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

            return await _baseRepo.FindManyAsync(filter);
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
            var result = await _baseRepo.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting old readings", ex);
        }
    }

    public async Task<Reading?> GetLatestBySensorIdsAsync(List<string> sensorIds)
    {
        try
        {
            var filter = Builders<ReadingDocument>.Filter.In(r => r.SensorId, sensorIds);
            var sort = Builders<ReadingDocument>.Sort.Descending(r => r.Timestamp);
            return await _baseRepo.FindLastOneAsync(filter,sort);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving latest reading", ex);
        }
    }
}
