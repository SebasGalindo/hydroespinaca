using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoSensorAlertRepository : ISensorAlertRepository
{
    private readonly BaseMongoRepository<SensorAlert, SensorAlertDocument> _baseRepo;

    public MongoSensorAlertRepository(MongoDbContext ctx, IEntityMapper<SensorAlert, SensorAlertDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<SensorAlert, SensorAlertDocument>(ctx.Database, "sensor_alerts", mapper);
    }

    public async Task CreateAsync(SensorAlert alert)
    {
        await _baseRepo.CreateAsync(alert);
    }

    public async Task<SensorAlert?> GetByIdAsync(string id)
    {
        return await _baseRepo.GetByIdAsync(id);
    }

    public async Task<List<SensorAlert>> GetBySensorIdAsync(string sensorId)
    {
        var filter = Builders<SensorAlertDocument>.Filter.Eq(x => x.SensorId, sensorId);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task UpdateAsync(SensorAlert sensorAlert)
    {
        await _baseRepo.UpdateAsync(sensorAlert);
    }

    public async Task<SensorAlert?> GetUnacknowledgedBySensorAndTypeAsync(string sensorId, AlertType type)
    {
        var filter = Builders<SensorAlertDocument>.Filter.And(
            Builders<SensorAlertDocument>.Filter.Eq(a => a.SensorId, sensorId),
            Builders<SensorAlertDocument>.Filter.Eq(a => a.Type, type),
            Builders<SensorAlertDocument>.Filter.Eq(a => a.Acknowledged, false)
        );

        return await _baseRepo.FindOneAsync(filter);
    }
}
