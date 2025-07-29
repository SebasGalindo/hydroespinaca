using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoEsp32AlertRepository : IEsp32AlertRepository
{
    private readonly BaseMongoRepository<Esp32Alert, Esp32AlertDocument> _baseRepo;

    public MongoEsp32AlertRepository(MongoDbContext ctx, IEntityMapper<Esp32Alert, Esp32AlertDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Esp32Alert, Esp32AlertDocument>(ctx.Database, "esp32_alerts", mapper);
    }

    public async Task CreateAsync(Esp32Alert alert)
    {
        await _baseRepo.CreateAsync(alert);
    }

    public async Task<Esp32Alert?> GetByIdAsync(string id)
    {
        return await _baseRepo.GetByIdAsync(id);
    }

    public async Task<List<Esp32Alert>> GetByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<Esp32AlertDocument>.Filter.Eq(x => x.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task UpdateAsync(Esp32Alert alert)
    {
        await _baseRepo.UpdateAsync(alert);
    }

    public async Task<Esp32Alert?> GetUnacknowledgedByEsp32AndTypeAsync(string esp32Id, AlertType type)
    {
        var filter = Builders<Esp32AlertDocument>.Filter.And(
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Esp32Id, esp32Id),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Type, type),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Acknowledged, false)
        );

        return await _baseRepo.FindOneAsync(filter);
    }
}
