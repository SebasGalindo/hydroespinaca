using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Exceptions;
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
        try
        {
            await _baseRepo.CreateAsync(alert);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error creating sensor alert", ex);
        }
    }

    public async Task<List<SensorAlert>> GetBySensorIdAsync(string sensorId)
    {
        try
        {
            var filter = Builders<SensorAlertDocument>.Filter.Eq(x => x.SensorId, sensorId);
            return await _baseRepo.FindManyAsync(filter);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving sensor alerts", ex);
        }
    }

    public async Task UpdateAcknowledgedAsync(string alertId, bool acknowledged)
    {
        try
        {
            await _baseRepo.UpdateFieldAsync(alertId, x => x.Acknowledged, acknowledged);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error updating alert acknowledgement", ex);
        }
    }

}
