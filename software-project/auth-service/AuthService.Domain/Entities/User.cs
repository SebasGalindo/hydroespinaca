using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Domain.Entities;
public class User : IIdentifiableMutable
{
    public string Id { get; private set; }
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public string? RoleId { get; private set; }

    public User(Email email, HashedPassword password, string? roleId = null)
    {
        Id = Guid.NewGuid().ToString();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RoleId = roleId;
    }
    public void SetId(string id)
    {
        Id = id;
    }

    public void UpdateEmail(Email email)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }

    public void UpdatePassword(HashedPassword password)
    {
        Password = password ?? throw new ArgumentNullException(nameof(password));
    }

    public void UpdateRoleId(string? roleId)
    {
        RoleId = roleId;
    }
}
