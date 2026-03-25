using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;

namespace NotificationService.Infrastructure.Persistence;

// Repositorio usando BaseMongoRepository para consistencia
public class MongoEmailLogRepository : IEmailLogRepository
{
    private readonly BaseMongoRepository<EmailLog, EmailLogDocument> _base;
    private readonly IMongoCollection<EmailLogDocument> _col;

    public MongoEmailLogRepository(IMongoDatabase db)
    {
        _base = new BaseMongoRepository<EmailLog, EmailLogDocument>(db, "email_logs", new EmailLogMapper());
        _col = db.GetCollection<EmailLogDocument>("email_logs");
        var correlationIndex = Builders<EmailLogDocument>.IndexKeys.Ascending(x => x.CorrelationId);
        _col.Indexes.CreateOne(new CreateIndexModel<EmailLogDocument>(correlationIndex, new CreateIndexOptions { Unique = true }));
        _col.Indexes.CreateOne(new CreateIndexModel<EmailLogDocument>(Builders<EmailLogDocument>.IndexKeys.Ascending(x => x.CreatedAt)));
    }

    public Task InsertAsync(EmailLog log, CancellationToken ct = default)
    {
        // Aseguramos Id = CorrelationId para queries consistentes
        if (string.IsNullOrEmpty(log.Id))
            log = new EmailLog { Id = log.CorrelationId, CorrelationId = log.CorrelationId, To = log.To, Subject = log.Subject, Status = log.Status, Provider = log.Provider, ProviderMessageId = log.ProviderMessageId, Error = log.Error, CreatedAt = log.CreatedAt, UpdatedAt = log.UpdatedAt };
        return _base.CreateAsync(log);
    }

    public Task UpdateStatusAsync(string correlationId, EmailDeliveryStatus status, string? provider = null, string? providerMessageId = null, string? error = null, CancellationToken ct = default)
    {
        var update = Builders<EmailLogDocument>.Update
            .Set(x => x.Status, status)
            .Set(x => x.Provider, provider)
            .Set(x => x.ProviderMessageId, providerMessageId)
            .Set(x => x.Error, error)
            .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow);
        return _col.UpdateOneAsync(x => x.CorrelationId == correlationId, update, cancellationToken: ct);
    }
}
