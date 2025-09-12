namespace BffService.Domain.ValueObjects;

public record TokenInfo(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime? RefreshTokenExpiresAt = null
);

public record AuthenticationResult(
    TokenInfo TokenInfo,
    string UserId,
    string UserRole,
    List<string> Scopes
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