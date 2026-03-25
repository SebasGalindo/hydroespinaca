using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB repository implementation for internal routine records.
/// </summary>
public class MongoInternalRoutineRepository : IInternalRoutineRepository
{
    private readonly BaseMongoRepository<InternalRoutine, InternalRoutineDocument> _baseRepo;

    public MongoInternalRoutineRepository(
        MongoDbContext ctx,
        IEntityMapper<InternalRoutine, InternalRoutineDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<InternalRoutine, InternalRoutineDocument>(
            ctx.Database, "internal_routines", mapper);
    }

    public async Task<List<InternalRoutine>> GetActiveRoutinesAsync()
    {
        var filter = Builders<InternalRoutineDocument>.Filter.Eq(r => r.IsActive, true);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task AddAsync(InternalRoutine routine)
    {
        routine.CreatedAt = DateTime.UtcNow;
        routine.UpdatedAt = DateTime.UtcNow;
        return _baseRepo.CreateAsync(routine);
    }

    public Task<InternalRoutine?> GetByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public async Task<InternalRoutine?> GetByNameAsync(string name)
    {
        var filter = Builders<InternalRoutineDocument>.Filter.Eq(r => r.Name, name);
        var results = await _baseRepo.FindManyAsync(filter);
        return results.FirstOrDefault();
    }

    public Task UpdateAsync(InternalRoutine routine)
    {
        routine.UpdatedAt = DateTime.UtcNow;
        return _baseRepo.UpdateAsync(routine);
    }

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);
}
