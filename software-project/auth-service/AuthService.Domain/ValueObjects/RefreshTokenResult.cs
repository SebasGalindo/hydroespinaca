namespace AuthService.Domain.ValueObjects;
public record RefreshTokenResult(
       Guid UserId,
       string Email,
       string Role,
       string ClientId
);