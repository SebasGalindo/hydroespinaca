using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Generates tokens with scopes based on user permissions and role
    /// </summary>
    Task<TokenResult> GenerateTokensAsync(string userId, string email, string role, string? clientId, TokenType tokenType = TokenType.User);
    
    bool IsTokenValid(string token);
    
    // Legacy methods for backward compatibility
    [Obsolete("Use GenerateTokensAsync instead")]
    TokenResult GenerateTokens(string userId, string email, string role, string? clientId, TokenType tokenType = TokenType.User);
    
    [Obsolete("Use GenerateTokensAsync instead")]
    TokenResult GenerateTokens(string userId, string email, string role, string? clientId)
        => GenerateTokens(userId, email, role, clientId, TokenType.User);
}

public class TokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Role { get; set; }
    public string? ClientId { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
}