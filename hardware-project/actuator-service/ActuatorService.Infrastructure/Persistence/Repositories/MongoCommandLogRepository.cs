using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

namespace ActuatorService.Infrastructure.Persistence.Repositories;

public class MongoCommandLogRepository : ICommandLogRepository
{
    private readonly BaseMongoRepository<ActuatorCommand, ActuatorCommandDocument> _baseRepo;

    public MongoCommandLogRepository(
        MongoDbContext ctx,
        IEntityMapper<ActuatorCommand, ActuatorCommandDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<ActuatorCommand, ActuatorCommandDocument>(
            ctx.Database, "actuator-commands", mapper);
    }

    public Task AddAsync(ActuatorCommand command)
        => _baseRepo.CreateAsync(command);

    public async Task<List<ActuatorCommand>> GetByActuatorIdAsync(string actuatorId)
    {
        var filter = Builders<ActuatorCommandDocument>.Filter.Eq(x => x.ActuatorId, actuatorId);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<List<ActuatorCommand>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var filter = Builders<ActuatorCommandDocument>.Filter.And(
            Builders<ActuatorCommandDocument>.Filter.Gte(x => x.Timestamp, from),
            Builders<ActuatorCommandDocument>.Filter.Lte(x => x.Timestamp, to)
        );
        return await _baseRepo.FindManyAsync(filter);
    }
}
