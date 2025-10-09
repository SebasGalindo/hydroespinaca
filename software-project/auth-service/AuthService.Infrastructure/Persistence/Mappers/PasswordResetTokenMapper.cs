using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for PasswordResetToken domain entity and MongoDB document using shared interface
/// </summary>
public class PasswordResetTokenMapper : IEntityMapper<PasswordResetToken, PasswordResetTokenDocument>
{
    /// <summary>
    /// Maps domain entity to MongoDB document
    /// </summary>
    /// <param name="entity">Domain entity</param>
    /// <returns>MongoDB document</returns>
    public PasswordResetTokenDocument ToDocument(PasswordResetToken entity)
    {
        return new PasswordResetTokenDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Code = entity.Code,
            ExpiresAt = entity.ExpiresAt,
            IsUsed = entity.IsUsed,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Maps MongoDB document to domain entity
    /// </summary>
    /// <param name="document">MongoDB document</param>
    /// <returns>Domain entity</returns>
    public PasswordResetToken ToEntity(PasswordResetTokenDocument document)
    {
        // Use reflection to create entity with private constructor
        var entity = (PasswordResetToken)Activator.CreateInstance(typeof(PasswordResetToken), true)!;
        
        // Set properties using reflection since they have private setters
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.Id))!
            .SetValue(entity, document.Id);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.UserId))!
            .SetValue(entity, document.UserId);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.Code))!
            .SetValue(entity, document.Code);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt))!
            .SetValue(entity, document.ExpiresAt);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.IsUsed))!
            .SetValue(entity, document.IsUsed);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.CreatedAt))!
            .SetValue(entity, document.CreatedAt);

        return entity;
    }
}