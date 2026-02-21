using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// MongoDB implementation of the INotificationPreferenceRepository.
/// This repository manages user notification preferences, allowing retrieval 
/// and updates of preferences for different notification channels ( email, push, SMS) and specific subscriptions (e.g., weather alerts). 
/// It uses a MongoDB collection named "notification_preferences" to store the preferences,
/// and it creates a unique index on the user_id field to ensure that each user has only one set of preferences.
/// The repository provides methods to get preferences by user ID, create new preferences, update existing ones
/// and retrieve subscribers for specific fuzzy systems and daily summaries based on their preferences.
/// </summary>
public class MongoNotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly BaseMongoRepository<NotificationPreference, NotificationPreferenceDocument> _base;
    private readonly IMongoCollection<NotificationPreferenceDocument> _collection;
    private readonly NotificationPreferenceMapper _mapper = new();

    // Constructor initializes the MongoDB collection and sets up a unique index on user_id to ensure one preference document per user
    public MongoNotificationPreferenceRepository(IMongoDatabase database)
    {
        _base = new BaseMongoRepository<NotificationPreference, NotificationPreferenceDocument>(
            database,
            "notification_preferences",
            _mapper
        );
        _collection = database.GetCollection<NotificationPreferenceDocument>("notification_preferences");

        // Unique index on user_id
        var indexKeys = Builders<NotificationPreferenceDocument>.IndexKeys.Ascending(x => x.UserId);
        var indexOptions = new CreateIndexOptions { Unique = true };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<NotificationPreferenceDocument>(indexKeys, indexOptions));
    }

    /// <inheritdoc />
    public async Task<NotificationPreference?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var doc = await _collection.Find(d => d.UserId == userId).FirstOrDefaultAsync(ct);
        return doc != null ? _mapper.ToEntity(doc) : null;
    }

    /// <inheritdoc />
    public async Task<NotificationPreference> CreateAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        preference.CreatedAt = DateTime.UtcNow;
        preference.UpdatedAt = DateTime.UtcNow;
        await _base.CreateAsync(preference);
        return preference;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        preference.UpdateTimestamp();
        var doc = _mapper.ToDocument(preference);
        await _collection.ReplaceOneAsync(
            d => d.UserId == preference.UserId,
            doc,
            new ReplaceOptions { IsUpsert = false },
            ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationPreference>> GetSubscribersForFuzzySystemAsync(
        string fuzzySystemId, CancellationToken ct = default)
    {
        // Users subscribed to weather alerts who either:
        // 1. Have fuzzy_system_id matching exactly, or
        // 2. Have fuzzy_system_id = null (auto-detect → subscribes to any active system)
        var filter = Builders<NotificationPreferenceDocument>.Filter.And(
            Builders<NotificationPreferenceDocument>.Filter.Eq(
                d => d.WeatherAlertsSubscription.Enabled, true),
            Builders<NotificationPreferenceDocument>.Filter.Or(
                Builders<NotificationPreferenceDocument>.Filter.Eq(
                    d => d.WeatherAlertsSubscription.FuzzySystemId, fuzzySystemId),
                Builders<NotificationPreferenceDocument>.Filter.Eq(
                    d => d.WeatherAlertsSubscription.FuzzySystemId, null)
            )
        );

        var docs = await _collection.Find(filter).ToListAsync(ct);
        return docs.Select(_mapper.ToEntity);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationPreference>> GetDailySummarySubscribersAsync(CancellationToken ct = default)
    {
        var filter = Builders<NotificationPreferenceDocument>.Filter.Eq(
            d => d.DailySummary.Enabled, true);
        var docs = await _collection.Find(filter).ToListAsync(ct);
        return docs.Select(_mapper.ToEntity);
    }
}
