using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;

namespace AuthService.Domain.Entities;
/// <summary>
/// Represents a system user with authentication credentials and assigned roles.
/// </summary>
public class User : IIdentifiableMutable
{
    public string Id { get; private set; }
    public string Username { get; private set; }
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public string? RoleId { get; private set; }

    public User(string username, Email email, HashedPassword password, string? roleId = null)
    {
        Id = null!; // MongoDB will auto-generate _id on insert
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RoleId = roleId;
    }
    public void SetId(string id)
    {
        Id = id;
    }

    public void UpdateUsername(string username)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
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
