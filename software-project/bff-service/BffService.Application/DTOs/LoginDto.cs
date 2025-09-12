namespace BffService.Application.DTOs;


public record LoginResponseDto(
    string SessionId,
    string CsrfToken,
    string Message = "Login successful"
);

public record LogoutRequestDto(
    string SessionId
);

public record RefreshTokenRequestDto(
    string SessionId
);

public record SessionInfoDto(
    string SessionId,
    string UserId,
    string UserRole,
    List<string> Scopes,
    DateTime ExpiresAt,
    bool IsValid
);