using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Interfaces;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
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

    public async Task CreateAsync(Esp32Node node) =>
        await _baseRepo.CreateAsync(node);

    public async Task<bool> ExistsAsync(string id) =>
        await _baseRepo.ExistsAsync(id);

    public async Task<bool> UpdateStatusAsync(string id, Esp32Status status)
    {
        return await _baseRepo.UpdateFieldAsync(id, x => x.Status, status);
    }

    public async Task<bool> UpdateLastSeenAsync(string id, DateTime lastSeen)
    {
        return await _baseRepo.UpdateFieldAsync(id, x => x.LastSeen, lastSeen);
    }
}
