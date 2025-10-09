using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Exceptions;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoSensorRepository : ISensorRepository
{
    private readonly BaseMongoRepository<Sensor, SensorDocument> _baseRepo;

    public MongoSensorRepository(MongoDbContext ctx, IEntityMapper<Sensor, SensorDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Sensor, SensorDocument>(ctx.Database, "sensors", mapper);
    }

    public async Task<Sensor?> GetByIdAsync(string id)
    {
        return await _baseRepo.GetByIdAsync(id);
    }

    public async Task<List<Sensor>> GetAllAsync()
    {
        return await _baseRepo.GetAllAsync();
    }

    public async Task<List<Sensor>> GetSensorsByEsp32IdAsync(string esp32Id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SensorDocument>.Filter.Eq(s => s.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task CreateAsync(Sensor sensor)
    {
        await _baseRepo.CreateAsync(sensor);
    }

    public async Task UpdateAsync(Sensor sensor)
    {
        await _baseRepo.UpdateAsync(sensor);
    }

    public async Task DeleteAsync(string id)
    {
        await _baseRepo.DeleteAsync(id);
    }
}
