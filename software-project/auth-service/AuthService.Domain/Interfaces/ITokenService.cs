namespace AuthService.Domain.Interfaces;

public interface ITokenService
{
    TokenResult GenerateTokens(string userId, string email, string role, string? clientId);
    bool IsTokenValid(string token);
}

public class TokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Role { get; set; }
    public string? ClientId { get; set; }
}