using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;
public class ClientAppMapper : IEntityMapper<ClientApp, ClientAppDocument>
{
    public ClientAppDocument ToDocument(ClientApp entity)
    {
        return new ClientAppDocument
        {
            Id = entity.Id.ToString(),
            ClientId = entity.ClientId,
            SecretHash = entity.Secret.Value,
            Scopes = entity.Scopes.Cast<string>().ToList()
        };
    }

    public ClientApp ToEntity(ClientAppDocument document)
    {
        return new ClientApp(
            document.ClientId,
            new HashedPassword(document.SecretHash),
            document.Scopes
        );
    }
}