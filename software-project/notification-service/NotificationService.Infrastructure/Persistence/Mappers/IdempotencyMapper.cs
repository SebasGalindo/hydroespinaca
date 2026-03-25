using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

public class IdempotencyMapper : IEntityMapper<IdempotencyRecord, IdempotencyDocument>
{
    public IdempotencyRecord ToEntity(IdempotencyDocument d) => new()
    {
        Key = d.Key,
        CorrelationId = d.CorrelationId,
        CreatedAt = d.CreatedAt,
        ExpiresAt = d.ExpiresAt,
        Status = d.Status,
        ResponseJson = d.ResponseJson
    };

    public IdempotencyDocument ToDocument(IdempotencyRecord e) => new()
    {
        Id = e.Key,
        Key = e.Key,
        CorrelationId = e.CorrelationId,
        CreatedAt = e.CreatedAt,
        ExpiresAt = e.ExpiresAt,
        Status = e.Status,
        ResponseJson = e.ResponseJson
    };
}
