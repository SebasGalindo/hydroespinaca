using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoInternalRoutineRepository : IInternalRoutineRepository
{
    private readonly BaseMongoRepository<InternalRoutine, InternalRoutineDocument> _baseRepo;
    private readonly IMongoCollection<InternalRoutineDocument> _collection;

    public MongoInternalRoutineRepository(
        MongoDbContext ctx,
        IEntityMapper<InternalRoutine, InternalRoutineDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<InternalRoutine, InternalRoutineDocument>(
            ctx.Database, "internal_routines", mapper);
        _collection = ctx.Database.GetCollection<InternalRoutineDocument>("internal_routines");
    }

    public async Task<List<InternalRoutine>> GetActiveRoutinesAsync()
    {
        var filter = Builders<InternalRoutineDocument>.Filter.Eq(r => r.IsActive, true);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task UpdateLastExecutedAtAsync(string id, DateTime timestamp)
    {
        var filter = Builders<InternalRoutineDocument>.Filter.Eq(r => r.Id, id);
        var update = Builders<InternalRoutineDocument>.Update
            .Set(r => r.LastExecutedAt, timestamp)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update);
    }

    public Task AddAsync(InternalRoutine routine)
    {
        routine.CreatedAt = DateTime.UtcNow;
        routine.UpdatedAt = DateTime.UtcNow;
        return _baseRepo.CreateAsync(routine);
    }

    public Task<InternalRoutine?> GetByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public Task UpdateAsync(InternalRoutine routine)
    {
        routine.UpdatedAt = DateTime.UtcNow;
        return _baseRepo.UpdateAsync(routine);
    }

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);
}
