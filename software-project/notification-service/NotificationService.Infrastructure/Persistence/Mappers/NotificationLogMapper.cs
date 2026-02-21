using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between NotificationLog domain entities and NotificationLogDocument MongoDB documents.
/// </summary>
public class NotificationLogMapper : IEntityMapper<NotificationLog, NotificationLogDocument>
{
    /// <summary>
    /// Converts a NotificationLogDocument from MongoDB into a NotificationLog domain entity, which is used in the application logic.
    /// </summary>
    public NotificationLog ToEntity(NotificationLogDocument document) => new()
    {
        Id = document.Id,
        CorrelationId = document.CorrelationId,
        UserId = document.UserId,
        Channel = document.Channel,
        TemplateKey = document.TemplateKey,
        Title = document.Title,
        Status = document.Status,
        Provider = document.Provider,
        ProviderMessageId = document.ProviderMessageId,
        Error = document.Error,
        CreatedAt = document.CreatedAt,
        UpdatedAt = document.UpdatedAt
    };

    /// <summary>
    /// Converts a NotificationLog domain entity into a NotificationLogDocument for storage in MongoDB. This is used when saving or updating notification logs in the database.
    /// </summary>
    public NotificationLogDocument ToDocument(NotificationLog entity) => new()
    {
        Id = entity.Id,
        CorrelationId = entity.CorrelationId,
        UserId = entity.UserId,
        Channel = entity.Channel,
        TemplateKey = entity.TemplateKey,
        Title = entity.Title,
        Status = entity.Status,
        Provider = entity.Provider,
        ProviderMessageId = entity.ProviderMessageId,
        Error = entity.Error,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
