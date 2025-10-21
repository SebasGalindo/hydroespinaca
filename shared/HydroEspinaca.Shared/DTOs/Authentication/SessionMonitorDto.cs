namespace HydroEspinaca.Shared.DTOs.Authentication;

public class SessionMonitorDto
{
    public string SessionId { get; init; } = default!;
    public string ClientId { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime LastActivity { get; init; }
    public bool Revoked { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public class UserSessionsDto
{
    public string UserId { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public List<SessionMonitorDto> Sessions { get; init; } = new();
}
