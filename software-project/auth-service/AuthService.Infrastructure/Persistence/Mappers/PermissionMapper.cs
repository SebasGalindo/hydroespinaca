using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;

/// <summary>
/// Mapper for converting between Permission domain entities and MongoDB documents.
/// </summary>
public class PermissionMapper : IEntityMapper<Permission, PermissionDocument>
{
    public Permission ToEntity(PermissionDocument doc)
    {
        var permission = new Permission(doc.Code, doc.Name, doc.Description);
        permission.SetId(doc.Id);
        return permission;
    }

    public PermissionDocument ToDocument(Permission entity)
    {
        return new PermissionDocument
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description
        };
    }
}