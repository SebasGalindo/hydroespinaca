using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoRoutineCommandRepository : IRoutineCommandRepository
{
    private readonly BaseMongoRepository<RoutineCommand, RoutineCommandDocument> _baseRepo;

    public MongoRoutineCommandRepository(MongoDbContext ctx,
                                         IEntityMapper<RoutineCommand, RoutineCommandDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<RoutineCommand, RoutineCommandDocument>(ctx.Database, "routine_commands", mapper);
    }

    public async Task<RoutineCommand?> GetByCommandIdAsync(string commandId)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.CommandId, commandId);
        var result = await _baseRepo.FindOneAsync(filter);
        return result;
    }

    public async Task<List<RoutineCommand>> GetByRoutineIdAsync(string routineId)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.RoutineId, routineId);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<List<RoutineCommand>> GetAllAsync()
    {
        return await _baseRepo.FindManyAsync(FilterDefinition<RoutineCommandDocument>.Empty);
    }

    public async Task<List<RoutineCommand>> GetByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Eq(x => x.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task AddAsync(RoutineCommand routineCommand)
        => _baseRepo.CreateAsync(routineCommand);

    public Task UpdateAsync(RoutineCommand routineCommand)
        => _baseRepo.UpdateAsync(routineCommand);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);

    public async Task<long> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        var filter = Builders<RoutineCommandDocument>.Filter.Lt(x => x.CreatedAt, cutoffDate);
        var result = await _baseRepo.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
}