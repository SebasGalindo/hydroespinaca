using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// MongoDB implementation of the IPushSubscriptionRepository.
/// This repository manages push notification subscriptions for users, 
/// allowing creation, retrieval, updating and deletion of subscriptions.
/// </summary>
public class MongoPushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly BaseMongoRepository<PushSubscription, PushSubscriptionDocument> _base;
    private readonly IMongoCollection<PushSubscriptionDocument> _collection;
    private readonly PushSubscriptionMapper _mapper = new();

    // Constructor initializes the MongoDB collection and sets up a unique index on user_id + token to prevent duplicate registrations
    public MongoPushSubscriptionRepository(IMongoDatabase database)
    {
        _base = new BaseMongoRepository<PushSubscription, PushSubscriptionDocument>(
            database,
            "push_subscriptions",
            _mapper
        );
        _collection = database.GetCollection<PushSubscriptionDocument>("push_subscriptions");

        // Compound index on user_id + token (unique to prevent duplicate registrations)
        var indexKeys = Builders<PushSubscriptionDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.Token);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<PushSubscriptionDocument>(indexKeys, indexOptions));
    }

    /// <inheritdoc />
    public async Task<PushSubscription> CreateAsync(PushSubscription subscription, CancellationToken ct = default)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        subscription.LastUsedAt = DateTime.UtcNow;
        await _base.CreateAsync(subscription);
        return subscription;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string subscriptionId, CancellationToken ct = default)
    {
        var result = await _collection.DeleteOneAsync(
            d => d.Id == subscriptionId, ct);
        return result.DeletedCount > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PushSubscription>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var docs = await _collection.Find(d => d.UserId == userId).ToListAsync(ct);
        return docs.Select(_mapper.ToEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PushSubscription>> GetActiveByUserIdAsync(
        string userId, string? platform = null, CancellationToken ct = default)
    {
        var filterBuilder = Builders<PushSubscriptionDocument>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(d => d.UserId, userId),
            filterBuilder.Eq(d => d.IsActive, true)
        );

        if (!string.IsNullOrEmpty(platform))
            filter = filterBuilder.And(filter, filterBuilder.Eq(d => d.Platform, platform));

        var docs = await _collection.Find(filter).ToListAsync(ct);
        return docs.Select(_mapper.ToEntity);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PushSubscription subscription, CancellationToken ct = default)
    {
        var doc = _mapper.ToDocument(subscription);
        await _collection.ReplaceOneAsync(
            d => d.Id == subscription.Id,
            doc,
            cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<int> DeactivateFailedSubscriptionsAsync(int maxFailures = 3, CancellationToken ct = default)
    {
        var filter = Builders<PushSubscriptionDocument>.Filter.And(
            Builders<PushSubscriptionDocument>.Filter.Gte(d => d.FailureCount, maxFailures),
            Builders<PushSubscriptionDocument>.Filter.Eq(d => d.IsActive, true)
        );
        var update = Builders<PushSubscriptionDocument>.Update.Set(d => d.IsActive, false);
        var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return (int)result.ModifiedCount;
    }

    /// <inheritdoc />
    public async Task<int> CleanupInactiveAsync(int olderThanDays = 90, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var filter = Builders<PushSubscriptionDocument>.Filter.And(
            Builders<PushSubscriptionDocument>.Filter.Eq(d => d.IsActive, false),
            Builders<PushSubscriptionDocument>.Filter.Lt(d => d.CreatedAt, cutoff)
        );
        var result = await _collection.DeleteManyAsync(filter, ct);
        return (int)result.DeletedCount;
    }
}
