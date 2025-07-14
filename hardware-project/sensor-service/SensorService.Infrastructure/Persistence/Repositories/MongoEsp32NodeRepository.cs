using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Mappers;
using SensorService.Infrastructure.Persistence.Models;
using Microsoft.Extensions.Configuration;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoEsp32NodeRepository : IEsp32NodeRepository
{
    private readonly IMongoCollection<Esp32NodeDocument> _collection;

    public MongoEsp32NodeRepository(IConfiguration config)
    {
        var client = new MongoClient(config["Mongo:ConnectionString"]);
        var db = client.GetDatabase(config["Mongo:Database"]);
        _collection = db.GetCollection<Esp32NodeDocument>("esp32_nodes");
    }

    public async Task<List<Esp32Node>> GetAllAsync() =>
        (await _collection.Find(_ => true).ToListAsync()).Select(Esp32NodeMapper.ToEntity).ToList();

    public async Task<Esp32Node?> GetByIdAsync(string id) =>
        Esp32NodeMapper.ToEntity(await _collection.Find(d => d.Id == id).FirstOrDefaultAsync());

    public async Task CreateAsync(Esp32Node node) =>
        await _collection.InsertOneAsync(Esp32NodeMapper.ToDocument(node));

    public async Task UpdateLastSeenAsync(string id, DateTime lastSeen) =>
        await _collection.UpdateOneAsync(d => d.Id == id, Builders<Esp32NodeDocument>.Update.Set(d => d.LastSeen, lastSeen));

    public async Task UpdateStatusAsync(string id, string status) =>
        await _collection.UpdateOneAsync(d => d.Id == id, Builders<Esp32NodeDocument>.Update.Set(d => d.Status, status));
}