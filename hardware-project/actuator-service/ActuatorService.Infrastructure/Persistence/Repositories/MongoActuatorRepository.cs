using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoActuatorRepository : IActuatorRepository
{
    private readonly BaseMongoRepository<Actuator, ActuatorDocument> _baseRepo;

    public MongoActuatorRepository(MongoDbContext ctx,
                                   IEntityMapper<Actuator, ActuatorDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Actuator, ActuatorDocument>(ctx.Database, "actuators", mapper);
    }


    public Task<Actuator?> GetByIdAsync(string id)
        => _baseRepo.GetByIdAsync(id);

    public async Task<List<Actuator>> GetByIdsAsync(List<string> ids)
    {
        var filter = Builders<ActuatorDocument>.Filter.In(x => x.Id, ids);
        return await _baseRepo.FindManyAsync(filter);
    }

    public Task<List<Actuator>> GetAllAsync()
        => _baseRepo.GetAllAsync();

    public Task AddAsync(Actuator actuator)
        => _baseRepo.CreateAsync(actuator);

    public Task UpdateAsync(Actuator actuator)
        => _baseRepo.UpdateAsync(actuator);

    public Task DeleteAsync(string id)
        => _baseRepo.DeleteAsync(id);

    public async Task<List<Actuator>> GetByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<ActuatorDocument>.Filter.Eq(x => x.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }
}
