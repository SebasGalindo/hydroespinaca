using SensorService.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;
using System.Dynamic;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoSensorRepository : ISensorRepository
{
    private readonly IMongoCollection<SensorDocument> _collection;

    public MongoSensorRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<SensorDocument>("sensors");
    }

    public async Task<Sensor?> GetByIdAsync(string id)
    {
        try
        {
            var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return doc is null ? null : SensorMapper.ToEntity(doc);
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
            var docs = await _collection.Find(_ => true).ToListAsync();
            return docs.Select(SensorMapper.ToEntity).ToList();
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
            var doc = SensorMapper.ToDocument(sensor);
            await _collection.InsertOneAsync(doc);

            sensor.Id = doc.Id;
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
            await _collection.ReplaceOneAsync(x => x.Id == sensor.Id, SensorMapper.ToDocument(sensor));
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
            await _collection.DeleteOneAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting sensor", ex);
        }
    }
}
