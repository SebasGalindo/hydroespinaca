using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between PushSubscription domain entities and PushSubscriptionDocument MongoDB documents.
/// </summary>
public class PushSubscriptionMapper : IEntityMapper<PushSubscription, PushSubscriptionDocument>
{
    /// <summary>
    /// Converts a PushSubscriptionDocument from MongoDB into a PushSubscription domain entity, which is used in the application logic.
    /// </summary>
    public PushSubscription ToEntity(PushSubscriptionDocument document) => new()
    {
        Id = document.Id,
        UserId = document.UserId,
        Platform = document.Platform,
        Token = document.Token,
        DeviceName = document.DeviceName,
        IsActive = document.IsActive,
        LastUsedAt = document.LastUsedAt,
        FailureCount = document.FailureCount,
        CreatedAt = document.CreatedAt
    };

    /// <summary>
    /// Converts a PushSubscription domain entity into a PushSubscriptionDocument for storage in MongoDB. This is used when saving or updating push subscriptions in the database.
    /// </summary>
    public PushSubscriptionDocument ToDocument(PushSubscription entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Platform = entity.Platform,
        Token = entity.Token,
        DeviceName = entity.DeviceName,
        IsActive = entity.IsActive,
        LastUsedAt = entity.LastUsedAt,
        FailureCount = entity.FailureCount,
        CreatedAt = entity.CreatedAt
    };
}
