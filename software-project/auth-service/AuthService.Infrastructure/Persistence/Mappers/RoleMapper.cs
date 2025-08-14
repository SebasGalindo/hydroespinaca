using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;

public class RoleMapper : IEntityMapper<Role, RoleDocument>
{
    public Role ToEntity(RoleDocument doc)
    {
        var role = new Role(doc.Code, doc.Name, doc.Permissions);
        role.SetId(doc.Id);
        return role;
    }

    public RoleDocument ToDocument(Role entity)
    {
        return new RoleDocument
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Permissions = entity.Permissions
        };
    }
}