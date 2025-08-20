namespace AuthService.Domain.ValueObjects;
public record RefreshTokenResult(
       string UserId,
       string Email,
       string Role,
       string ClientId
);