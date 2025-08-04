using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Domain.Entities;
public class RefreshToken : IIdentifiableMutable
{
    public string Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool Revoked { get; private set; }
    public string ClientId { get; private set; }

    public RefreshToken(Guid userId, string token, DateTime expiresAt, string clientId)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        Revoked = false;
        ClientId = clientId;
    }

    public void SetId(string id) => Id = id;

    public void Revoke() => Revoked = true;

    public bool IsActive => !Revoked && DateTime.UtcNow < ExpiresAt;
}