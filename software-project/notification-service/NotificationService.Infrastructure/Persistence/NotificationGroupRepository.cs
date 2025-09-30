using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationGroupRepository : INotificationGroupRepository
{
    private readonly BaseMongoRepository<NotificationGroup, NotificationGroupDocument> _base;
    private readonly IMongoCollection<NotificationGroupDocument> _collection;

    public NotificationGroupRepository(IMongoDatabase database)
    {
        _base = new BaseMongoRepository<NotificationGroup, NotificationGroupDocument>(
            database, 
            "notification_groups", 
            new NotificationGroupMapper()
        );
        
        _collection = database.GetCollection<NotificationGroupDocument>("notification_groups");
        
        // Create unique index on groupName for fast lookups and prevent duplicates
        var indexKeysDefinition = Builders<NotificationGroupDocument>.IndexKeys.Ascending(x => x.GroupName);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<NotificationGroupDocument>(indexKeysDefinition, indexOptions);
        
        _collection.Indexes.CreateOneAsync(indexModel);
    }

    public async Task<IEnumerable<NotificationGroup>> GetAllAsync(CancellationToken ct = default)
    {
        var documents = await _collection.Find(_ => true).ToListAsync(ct);
        var mapper = new NotificationGroupMapper();
        return documents.Select(mapper.ToEntity);
    }

    public async Task<NotificationGroup?> GetByGroupNameAsync(string groupName, CancellationToken ct = default)
    {
        var document = await _collection.Find(g => g.GroupName == groupName).FirstOrDefaultAsync(ct);
        if (document == null) return null;
        
        var mapper = new NotificationGroupMapper();
        return mapper.ToEntity(document);
    }

    public async Task<NotificationGroup> CreateAsync(NotificationGroup group, CancellationToken ct = default)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        
        await _base.CreateAsync(group);
        return group;
    }

    public async Task<NotificationGroup?> UpdateAsync(string groupName, NotificationGroup group, CancellationToken ct = default)
    {
        group.UpdateTimestamp();
        
        var filter = Builders<NotificationGroupDocument>.Filter.Eq(g => g.GroupName, groupName);
        var mapper = new NotificationGroupMapper();
        var document = mapper.ToDocument(group);
        
        var result = await _collection.ReplaceOneAsync(filter, document, cancellationToken: ct);
        
        return result.MatchedCount > 0 ? group : null;
    }

    public async Task<bool> DeleteAsync(string groupName, CancellationToken ct = default)
    {
        var filter = Builders<NotificationGroupDocument>.Filter.Eq(g => g.GroupName, groupName);
        var result = await _collection.DeleteOneAsync(filter, ct);
        
        return result.DeletedCount > 0;
    }

    public async Task<bool> ExistsAsync(string groupName, CancellationToken ct = default)
    {
        var filter = Builders<NotificationGroupDocument>.Filter.Eq(g => g.GroupName, groupName);
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        
        return count > 0;
    }
}