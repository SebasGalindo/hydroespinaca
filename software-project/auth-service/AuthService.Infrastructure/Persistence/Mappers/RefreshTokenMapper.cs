using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;
public class RefreshTokenMapper : IEntityMapper<RefreshToken, RefreshTokenDocument>
{
    public RefreshTokenDocument ToDocument(RefreshToken entity)
    {
        return new RefreshTokenDocument
        {
            Id = entity.Id.ToString(),
            UserId = entity.UserId,
            Token = entity.Token,
            ExpiresAt = entity.ExpiresAt,
            Revoked = entity.Revoked,
            ClientId = entity.ClientId
        };
    }

    public RefreshToken ToEntity(RefreshTokenDocument document)
    {
        var token = new RefreshToken(
            document.UserId,
            document.Token,
            document.ExpiresAt,
            document.ClientId
        );

        token.SetId(document.Id);

        if (document.Revoked)
            token.Revoke();

        return token;
    }
}