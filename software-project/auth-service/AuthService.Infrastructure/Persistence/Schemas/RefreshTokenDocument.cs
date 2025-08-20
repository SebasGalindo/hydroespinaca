using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Infrastructure.Persistence.Schemas;
public class RefreshTokenDocument : IIdentifiableMutable
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public string ClientId { get; set; } = default!;

    public void SetId(string id) => Id = id;
}