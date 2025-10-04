using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoControlOutputRepository : IControlOutputRepository
{
    private readonly BaseMongoRepository<ControlOutput, ControlOutputDocument> _baseRepo;

    public MongoControlOutputRepository(MongoDbContext ctx,
                                        IEntityMapper<ControlOutput, ControlOutputDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<ControlOutput, ControlOutputDocument>(ctx.Database, "control_outputs", mapper);
    }

    public Task<ControlOutput?> GetByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public Task<List<ControlOutput>> GetAllAsync()
        => _baseRepo.GetAllAsync();

    public async Task<List<ControlOutput>> GetByActuatorIdAsync(string actuatorId)
    {
        var filter = Builders<ControlOutputDocument>.Filter.Eq(x => x.ActuatorId, actuatorId);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task AddAsync(ControlOutput controlOutput)
    {
        controlOutput.LastModified = DateTime.UtcNow;
        return _baseRepo.CreateAsync(controlOutput);
    }

    public Task UpdateAsync(ControlOutput controlOutput)
    {
        controlOutput.LastModified = DateTime.UtcNow;
        return _baseRepo.UpdateAsync(controlOutput);
    }

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);
}
