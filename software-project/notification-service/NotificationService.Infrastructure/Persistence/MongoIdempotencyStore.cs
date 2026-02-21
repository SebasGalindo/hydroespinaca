using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

public class MongoIdempotencyStore : IIdempotencyStore
{
    private readonly BaseMongoRepository<IdempotencyRecord, IdempotencyDocument> _base;
    private readonly IMongoCollection<IdempotencyDocument> _col;

    public MongoIdempotencyStore(IMongoDatabase db)
    {
        _base = new BaseMongoRepository<IdempotencyRecord, IdempotencyDocument>(db, "idempotency", new IdempotencyMapper());
        _col = db.GetCollection<IdempotencyDocument>("idempotency");
        var keys = Builders<IdempotencyDocument>.IndexKeys.Ascending(x => x.ExpiresAt);
        _col.Indexes.CreateOne(new CreateIndexModel<IdempotencyDocument>(keys, new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl_exp" }));
    }

    public async Task<(bool acquired, IdempotencyRecord record)> TryReserveAsync(string key, string correlationId, TimeSpan ttl, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new IdempotencyRecord
        {
            Key = key,
            CorrelationId = correlationId,
            CreatedAt = now,
            ExpiresAt = now.Add(ttl),
            Status = IdempotencyStatus.Reserved,
            ResponseJson = null
        };
        try
        {
            await _base.CreateAsync(record);
            return (true, record);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await _col.Find(d => d.Id == key).FirstOrDefaultAsync(ct);
            var mapper = new IdempotencyMapper();
            return (false, mapper.ToEntity(existing!));
        }
    }

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        var doc = await _col.Find(d => d.Id == key).FirstOrDefaultAsync(ct);
        return doc is null ? null : new IdempotencyMapper().ToEntity(doc);
    }

    public async Task UpdateAsync(string key, Action<IdempotencyRecord> update, CancellationToken ct = default)
    {
        var existing = await _col.Find(d => d.Id == key).FirstOrDefaultAsync(ct);
        if (existing is null) return;
        var mapper = new IdempotencyMapper();
        var entity = mapper.ToEntity(existing);
        update(entity);
        var replace = mapper.ToDocument(entity);
        await _col.ReplaceOneAsync(d => d.Id == key, replace, cancellationToken: ct);
    }
}
