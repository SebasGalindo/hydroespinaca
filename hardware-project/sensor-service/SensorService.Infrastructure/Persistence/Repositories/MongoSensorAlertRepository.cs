using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;
public class MongoSensorAlertRepository : ISensorAlertRepository
{
    private readonly IMongoCollection<SensorAlertDocument> _collection;

    public MongoSensorAlertRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<SensorAlertDocument>("sensor_alerts");
    }

    public async Task CreateAsync(SensorAlert alert)
    {
        var doc = SensorAlertMapper.ToDocument(alert);
        await _collection.InsertOneAsync(doc);
    }

    public async Task<List<SensorAlert>> GetBySensorIdAsync(string sensorId)
    {
        var filter = Builders<SensorAlertDocument>.Filter.Eq(x => x.SensorId, sensorId);
        var docs = await _collection.Find(filter).ToListAsync();
        return docs.Select(SensorAlertMapper.ToEntity).ToList();
    }

    public async Task AcknowledgeAsync(string alertId)
    {
        var update = Builders<SensorAlertDocument>.Update.Set(x => x.Acknowledged, true);
        await _collection.UpdateOneAsync(x => x.Id == alertId, update);
    }
}
