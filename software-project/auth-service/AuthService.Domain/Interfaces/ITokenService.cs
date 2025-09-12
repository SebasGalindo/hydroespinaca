using AuthService.Domain.Enums;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Domain.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Generates tokens with scopes based on user permissions and role
    /// </summary>
    Task<TokenResult> GenerateTokensAsync(string userId, string email, string role, string? clientId, TokenType tokenType = TokenType.User);
    
    /// <summary>
    /// Generates M2M tokens with explicit scopes from client app database
    /// </summary>
    Task<TokenResult> GenerateTokensAsync(string userId, string email, string role, string? clientId, TokenType tokenType, string[] explicitScopes);
    
    bool IsTokenValid(string token);
}

