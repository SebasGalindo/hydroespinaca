using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;
using WeatherService.Domain.Persistence.Documents;
using WeatherService.Infrastructure.Persistence.Mappers;

namespace WeatherService.Infrastructure.Persistence;

/// <summary>
/// Repository for managing weather alerts in MongoDB. 
/// This repository provides methods to create new alerts, 
/// retrieve alerts based on various filters (such as fuzzy system ID, user ID, date range, and read status), 
/// mark alerts as read for specific users, check for recent similar alerts to 
/// prevent duplicates, and add notified users to existing alerts. 
/// The implementation uses a base repository for common CRUD operations 
/// and directly interacts with the MongoDB collection for specific queries 
/// and updates related to weather alerts. 
/// Compound indexes are created to optimize queries for deduplication 
/// and user-specific alert retrieval.
/// </summary>
public class MongoWeatherAlertRepository : IWeatherAlertRepository
{
    private readonly BaseMongoRepository<WeatherAlert, WeatherAlertDocument> _base;
    private readonly IMongoCollection<WeatherAlertDocument> _collection;
    private readonly WeatherAlertMapper _mapper = new();

    public MongoWeatherAlertRepository(IMongoDatabase database)
    {
        // Initialize the base repository for common CRUD operations
        _base = new BaseMongoRepository<WeatherAlert, WeatherAlertDocument>(
            database, "weather_alerts", _mapper);

        _collection = database.GetCollection<WeatherAlertDocument>("weather_alerts");

        // Compound index for deduplication queries
        var dedupIndex = Builders<WeatherAlertDocument>.IndexKeys
            .Ascending(x => x.AlertType)
            .Ascending(x => x.FuzzySystemId)
            .Descending(x => x.CreatedAt);

        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<WeatherAlertDocument>(dedupIndex));

        // Index for user alert queries
        var userIndex = Builders<WeatherAlertDocument>.IndexKeys
            .Ascending("notified_users.user_id")
            .Descending(x => x.CreatedAt);

        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<WeatherAlertDocument>(userIndex));
    }

    /// <inheritdoc/>
    public async Task<WeatherAlert> CreateAsync(WeatherAlert alert, CancellationToken ct = default)
    {
        alert.CreatedAt = DateTime.UtcNow;
        await _base.CreateAsync(alert);
        return alert;
    }

    /// <inheritdoc/>
    public async Task<List<WeatherAlert>> GetByFiltersAsync(
        string? fuzzySystemId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var builder = Builders<WeatherAlertDocument>.Filter;
        var filters = new List<FilterDefinition<WeatherAlertDocument>>();

        if (!string.IsNullOrEmpty(fuzzySystemId))
            filters.Add(builder.Eq(x => x.FuzzySystemId, fuzzySystemId));

        if (!string.IsNullOrEmpty(userId))
            filters.Add(builder.ElemMatch(x => x.NotifiedUsers,
                Builders<NotifiedUserDocument>.Filter.Eq(n => n.UserId, userId)));

        if (from.HasValue)
            filters.Add(builder.Gte(x => x.CreatedAt, from.Value));

        if (to.HasValue)
            filters.Add(builder.Lte(x => x.CreatedAt, to.Value));

        if (unreadOnly == true && !string.IsNullOrEmpty(userId))
            filters.Add(builder.ElemMatch(x => x.NotifiedUsers,
                Builders<NotifiedUserDocument>.Filter.And(
                    Builders<NotifiedUserDocument>.Filter.Eq(n => n.UserId, userId),
                    Builders<NotifiedUserDocument>.Filter.Eq(n => n.IsRead, false))));

        var filter = filters.Count > 0
            ? builder.And(filters)
            : builder.Empty;

        var docs = await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(100)
            .ToListAsync(ct);

        return docs.Select(_mapper.ToEntity).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(string alertId, string userId, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertDocument>.Filter.And(
            Builders<WeatherAlertDocument>.Filter.Eq(x => x.Id, alertId),
            Builders<WeatherAlertDocument>.Filter.ElemMatch(x => x.NotifiedUsers,
                Builders<NotifiedUserDocument>.Filter.Eq(n => n.UserId, userId)));

        var update = Builders<WeatherAlertDocument>.Update
            .Set("notified_users.$.is_read", true);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.MatchedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsRecentAsync(string alertType, string fuzzySystemId, int deduplicationHours, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-deduplicationHours);

        var filter = Builders<WeatherAlertDocument>.Filter.And(
            Builders<WeatherAlertDocument>.Filter.Eq(x => x.AlertType, alertType),
            Builders<WeatherAlertDocument>.Filter.Eq(x => x.FuzzySystemId, fuzzySystemId),
            Builders<WeatherAlertDocument>.Filter.Gte(x => x.CreatedAt, cutoff));

        return await _collection.Find(filter).Limit(1).AnyAsync(ct);
    }

    /// <inheritdoc/>
    public async Task AddNotifiedUserAsync(string alertId, NotifiedUser notifiedUser, CancellationToken ct = default)
    {
        var filter = Builders<WeatherAlertDocument>.Filter.Eq(x => x.Id, alertId);
        var notifiedDoc = new NotifiedUserDocument
        {
            UserId = notifiedUser.UserId,
            Channels = notifiedUser.Channels,
            SentAt = notifiedUser.SentAt,
            IsRead = notifiedUser.IsRead
        };

        var update = Builders<WeatherAlertDocument>.Update
            .Push(x => x.NotifiedUsers, notifiedDoc);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}
