using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

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

    public async Task<IEnumerable<Sensor>> GetAllAsync()
    {
        var docs = await _collection.Find(_ => true).ToListAsync();
        return docs.Select(SensorMapper.ToEntity);
    }

    public async Task<Sensor?> GetByIdAsync(string id)
    {
        var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        return doc is null ? null : SensorMapper.ToEntity(doc);
    }

    public async Task AddAsync(Sensor sensor)
    {
        await _collection.InsertOneAsync(SensorMapper.ToDocument(sensor));
    }

    public async Task UpdateAsync(Sensor sensor)
    {
        await _collection.ReplaceOneAsync(x => x.Id == sensor.Id, SensorMapper.ToDocument(sensor));
    }

    public async Task DeleteAsync(string id)
    {
        await _collection.DeleteOneAsync(x => x.Id == id);
    }
}
