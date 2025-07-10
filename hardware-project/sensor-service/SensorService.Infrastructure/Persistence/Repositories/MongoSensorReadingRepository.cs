using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;
public class MongoSensorReadingRepository : ISensorReadingRepository
{
    private readonly IMongoCollection<SensorReadingDocument> _collection;

    public MongoSensorReadingRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<SensorReadingDocument>("sensor_readings");
    }

    public async Task SaveAsync(SensorReading data)
    {
        var doc = SensorReadingMapper.ToDocument(data);
        await _collection.InsertOneAsync(doc);
    }
}
