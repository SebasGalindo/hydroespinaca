namespace BffService.Domain.ValueObjects;

/// <summary>
/// Value object holding access token, refresh token, and expiration metadata for a user session.
/// </summary>
public record TokenInfo(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime? RefreshTokenExpiresAt = null
);

public record AuthenticationResult(
    TokenInfo TokenInfo,
    string UserId,
    string Username,
    string Email,
    string UserRole,
    List<string> Scopes,
    bool HasAcceptedTerms = false
);

public record ProxyRequest(
    string Method,
    string Path,
    Dictionary<string, string> Headers,
    string? Body = null
);

public record ProxyResponse(
    int StatusCode,
    Dictionary<string, string> Headers,
    string? Body = null
);