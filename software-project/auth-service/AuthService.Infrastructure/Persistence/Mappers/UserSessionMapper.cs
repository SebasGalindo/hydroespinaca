using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;

public class UserSessionMapper : IEntityMapper<UserSession, UserSessionDocument>
{
    public UserSessionDocument ToDocument(UserSession entity)
    {
        return new UserSessionDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ClientId = entity.ClientId,
            SessionId = entity.SessionId,
            RefreshToken = entity.RefreshToken,
            AccessTokenHash = entity.AccessTokenHash,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            LastActivity = entity.LastActivity,
            Revoked = entity.Revoked,
            RevokedAt = entity.RevokedAt,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CsrfToken = entity.CsrfToken
        };
    }

    public UserSession ToEntity(UserSessionDocument document)
    {
        var entity = new UserSession(
            document.UserId,
            document.ClientId,
            document.SessionId,
            document.RefreshToken,
            document.AccessTokenHash,
            document.ExpiresAt,
            document.IpAddress,
            document.UserAgent,
            document.CsrfToken
        );

        entity.SetId(document.Id);

        // Restore state
        if (document.Revoked)
        {
            entity.Revoke();
        }

        return entity;
    }
}
