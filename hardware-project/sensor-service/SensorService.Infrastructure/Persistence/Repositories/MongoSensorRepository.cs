using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
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
        try
        {
            return await _baseRepo.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving sensor by ID", ex);
        }
    }

    public async Task<List<Sensor>> GetAllAsync()
    {
        try
        {
            return await _baseRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving all sensors", ex);
        }
    }

    public async Task CreateAsync(Sensor sensor)
    {
        try
        {
            await _baseRepo.CreateAsync(sensor);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error creating sensor", ex);
        }
    }

    public async Task UpdateAsync(Sensor sensor)
    {
        try
        {
            await _baseRepo.UpdateAsync(sensor);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error updating sensor", ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _baseRepo.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting sensor", ex);
        }
    }
}
