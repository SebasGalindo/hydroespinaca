using ActuatorService.Domain.Entities;
using ActuatorService.Domain.Interfaces;
using ActuatorService.Infrastructure.Persistence.Models;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;

public class MongoCommandLogRepository
    : BaseMongoRepository<ActuatorCommand, ActuatorCommandDocument>, ICommandLogRepository
{
    public MongoCommandLogRepository(
        MongoDbContext ctx,
        IEntityMapper<ActuatorCommand, ActuatorCommandDocument> mapper)
        : base(ctx.Database, "actuator-commands", mapper)
    {
    }
    public async Task AddAsync(ActuatorCommand command)
    {
        await CreateAsync(command); 
    }
    public async Task<List<ActuatorCommand>> GetByActuatorIdAsync(string actuatorId)
    {
        var filter = Builders<ActuatorCommandDocument>.Filter.Eq(x => x.ActuatorId, actuatorId);
        var docs = await Collection.Find(filter).ToListAsync();
        return docs.Select(Mapper.ToEntity).ToList();
    }

    public async Task<List<ActuatorCommand>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var filter = Builders<ActuatorCommandDocument>.Filter.And(
            Builders<ActuatorCommandDocument>.Filter.Gte(x => x.Timestamp, from),
            Builders<ActuatorCommandDocument>.Filter.Lte(x => x.Timestamp, to)
        );

        var docs = await Collection.Find(filter).ToListAsync();
        return docs.Select(Mapper.ToEntity).ToList();
    }
}
