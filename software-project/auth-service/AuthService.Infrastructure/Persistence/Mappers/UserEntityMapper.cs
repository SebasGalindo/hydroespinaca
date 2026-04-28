using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo.Interfaces;

namespace AuthService.Infrastructure.Persistence.Mappers;
/// <summary>
/// Mapper for converting between User domain entities and MongoDB documents.
/// </summary>
public class UserMapper : IEntityMapper<User, UserDocument>
{
    public User ToEntity(UserDocument doc)
    {
        var user = new User(
            doc.Username,
            new Email(doc.Email),
            new HashedPassword(doc.Password),
            doc.RoleId
        );
        user.SetId(doc.Id);
        if (doc.HasAcceptedTerms)
            user.AcceptTerms();
        return user;
    }

    public UserDocument ToDocument(User entity)
    {
        return new UserDocument
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email.Value,
            Password = entity.Password.Value,
            RoleId = entity.RoleId,
            HasAcceptedTerms = entity.HasAcceptedTerms
        };
    }
}