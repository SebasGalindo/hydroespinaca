using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoEsp32NodeRepository : IEsp32NodeRepository
{
    private readonly BaseMongoRepository<Esp32Node, Esp32NodeDocument> _baseRepo;

    public MongoEsp32NodeRepository(MongoDbContext ctx,
                                    IEntityMapper<Esp32Node, Esp32NodeDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Esp32Node, Esp32NodeDocument>(
            ctx.Database, "esp32_nodes", mapper
        );
    }

    public async Task<List<Esp32Node>> GetAllAsync() =>
        await _baseRepo.GetAllAsync();

    public async Task<Esp32Node?> GetByIdAsync(string id) =>
        await _baseRepo.GetByIdAsync(id);

    public async Task<Esp32Node?> GetByIdentifierAsync(string identifier)
    {
        // First, try to get by ObjectId if the identifier is a valid ObjectId format
        if (ObjectId.TryParse(identifier, out var objectId))
        {
            return await _baseRepo.GetByIdAsync(identifier);
        }

        // If not a valid ObjectId, search by name (case-insensitive)
        // This handles cases like "esp32-001" which should match the 'name' field
        var filter = Builders<Esp32NodeDocument>.Filter.Regex(x => x.Name, 
            new BsonRegularExpression($"^{identifier}$", "i"));

        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task CreateAsync(Esp32Node node) =>
        await _baseRepo.CreateAsync(node);

    public async Task UpdateAsync(Esp32Node node) =>
        await _baseRepo.UpdateAsync(node);

    public async Task<bool> ExistsAsync(string id)
    {
        var filter = Builders<Esp32NodeDocument>.Filter.Eq(x => x.Id, id);
        return await _baseRepo.ExistsAsync(filter);
    }

    public async Task<bool> UpdateStatusAsync(string id, Esp32Status status)
    {
        return await _baseRepo.UpdateFieldAsync(id, x => x.Status, status);
    }

    public async Task<bool> UpdateLastSeenAsync(string id, DateTime lastSeen)
    {
        return await _baseRepo.UpdateFieldAsync(id, x => x.LastSeen, lastSeen);
    }
}
