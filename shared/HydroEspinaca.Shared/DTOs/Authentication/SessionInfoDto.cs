namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Response DTO containing session information from BFF service
/// </summary>
public record SessionInfoDto(
    string SessionId,
    string UserId,
    string UserRole,
    List<string> Scopes,
    DateTime ExpiresAt,
    bool IsValid
);