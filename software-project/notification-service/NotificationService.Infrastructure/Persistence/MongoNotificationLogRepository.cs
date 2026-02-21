using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// MongoDB implementation of the INotificationLogRepository. 
/// This repository is responsible for storing and retrieving notification logs, 
/// which contain records of all notifications sent to users, including their status and metadata. 
/// It uses a MongoDB collection named "notification_log" to persist the data, 
/// and it creates indexes on user_id and created_at for efficient querying of logs by user and time range. 
/// The repository provides methods to create new logs, update existing ones, 
/// and retrieve logs based on user ID with optional filtering by channel and date range.
/// </summary>
public class MongoNotificationLogRepository : INotificationLogRepository
{
    private readonly BaseMongoRepository<NotificationLog, NotificationLogDocument> _base;
    private readonly IMongoCollection<NotificationLogDocument> _collection;
    private readonly NotificationLogMapper _mapper = new();

    // Constructor initializes the MongoDB collection and sets up indexes for efficient querying
    public MongoNotificationLogRepository(IMongoDatabase database)
    {
        _base = new BaseMongoRepository<NotificationLog, NotificationLogDocument>(
            database,
            "notification_log",
            _mapper
        );
        _collection = database.GetCollection<NotificationLogDocument>("notification_log");

        // Compound index for user query: user_id + created_at (descending)
        var userIndex = Builders<NotificationLogDocument>.IndexKeys
            .Ascending(d => d.UserId)
            .Descending(d => d.CreatedAt);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<NotificationLogDocument>(userIndex));

        // Index on correlation_id for tracing
        var correlationIndex = Builders<NotificationLogDocument>.IndexKeys
            .Ascending(d => d.CorrelationId);
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<NotificationLogDocument>(correlationIndex));
    }

    /// <inheritdoc />
    public async Task<NotificationLog> CreateAsync(NotificationLog log, CancellationToken ct = default)
    {
        log.CreatedAt = DateTime.UtcNow;
        await _base.CreateAsync(log);
        return log;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(NotificationLog log, CancellationToken ct = default)
    {
        log.UpdatedAt = DateTime.UtcNow;
        var doc = _mapper.ToDocument(log);
        await _collection.ReplaceOneAsync(d => d.Id == log.Id, doc, cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationLog>> GetByUserIdAsync(
        string userId,
        string? channel = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        var filterBuilder = Builders<NotificationLogDocument>.Filter;
        var filters = new List<FilterDefinition<NotificationLogDocument>>
        {
            filterBuilder.Eq(d => d.UserId, userId)
        };

        if (!string.IsNullOrEmpty(channel))
            filters.Add(filterBuilder.Eq(d => d.Channel, channel));
        if (from.HasValue)
            filters.Add(filterBuilder.Gte(d => d.CreatedAt, from.Value));
        if (to.HasValue)
            filters.Add(filterBuilder.Lte(d => d.CreatedAt, to.Value));

        var filter = filterBuilder.And(filters);

        var docs = await _collection
            .Find(filter)
            .SortByDescending(d => d.CreatedAt)
            .Limit(limit)
            .ToListAsync(ct);

        return docs.Select(_mapper.ToEntity);
    }
}
