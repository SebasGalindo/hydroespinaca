using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Abstractions;
using HydroEspinaca.Shared.Enums;

namespace AuthService.Domain.Entities;
public class User : IIdentifiableMutable
{
    public string Id { get; private set; }
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public Role Role { get; private set; }

    public User(Email email, HashedPassword password, Role role)
    {
        Id = Guid.NewGuid().ToString();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Role = role;
    }

    public bool VerifyPassword(string plainText, IPasswordHasher hasher)
    {
        return hasher.Verify(plainText, Password.Value);
    }

    public void SetId(string id)
    {
        Id = id;
    }
}
