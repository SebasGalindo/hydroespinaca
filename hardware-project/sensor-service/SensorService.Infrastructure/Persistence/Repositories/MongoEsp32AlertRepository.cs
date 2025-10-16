using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoEsp32AlertRepository : IEsp32AlertRepository
{
    private readonly BaseMongoRepository<Esp32Alert, Esp32AlertDocument> _baseRepo;

    public MongoEsp32AlertRepository(MongoDbContext ctx, IEntityMapper<Esp32Alert, Esp32AlertDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Esp32Alert, Esp32AlertDocument>(ctx.Database, "esp32_alerts", mapper);
    }

    public async Task CreateAsync(Esp32Alert alert)
    {
        await _baseRepo.CreateAsync(alert);
    }

    public async Task<Esp32Alert?> GetByIdAsync(string id)
    {
        return await _baseRepo.GetByIdAsync(id);
    }

    public async Task<List<Esp32Alert>> GetByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<Esp32AlertDocument>.Filter.Eq(x => x.Esp32Id, esp32Id);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task UpdateAsync(Esp32Alert alert)
    {
        await _baseRepo.UpdateAsync(alert);
    }

    public async Task<Esp32Alert?> GetActiveByEsp32IdAsync(string esp32Id)
    {
        var filter = Builders<Esp32AlertDocument>.Filter.And(
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Esp32Id, esp32Id),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Acknowledged, false),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.ResolvedAt, null)
        );

        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        var filter = Builders<Esp32AlertDocument>.Filter.Lt(a => a.Timestamp, cutoffDate);
        var result = await _baseRepo.DeleteManyAsync(filter);
        return (int)result.DeletedCount;
    }

    public async Task<List<Esp32Alert>> GetUnsentEmailAlertsByEsp32IdsAsync(IEnumerable<string> esp32Ids, CancellationToken cancellationToken = default)
    {
        var esp32IdList = esp32Ids.ToList();

        if (!esp32IdList.Any())
            return new List<Esp32Alert>();

        var filter = Builders<Esp32AlertDocument>.Filter.And(
            Builders<Esp32AlertDocument>.Filter.In(a => a.Esp32Id, esp32IdList),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.Acknowledged, false),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.ResolvedAt, null),
            Builders<Esp32AlertDocument>.Filter.Eq(a => a.EmailSentAt, null)
        );

        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task MarkEmailAsSentAsync(string alertId, DateTime sentAt, CancellationToken cancellationToken = default)
    {
        var alert = await _baseRepo.GetByIdAsync(alertId);
        if (alert == null)
            return;

        alert.EmailSentAt = sentAt;
        await _baseRepo.UpdateAsync(alert);
    }
}
