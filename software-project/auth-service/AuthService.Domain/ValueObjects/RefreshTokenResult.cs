namespace AuthService.Domain.ValueObjects;
/// <summary>
/// Value object containing the result of a refresh token generation operation.
/// </summary>
public record RefreshTokenResult(
       string UserId,
       string Email,
       string Role,
       string ClientId
);