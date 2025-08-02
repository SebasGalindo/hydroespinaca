using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;
public interface ITokenService
{
    /// <summary>
    /// Generates a token pair (access and refresh) for the authenticated user.
    /// </summary>
    /// <param name="userId">Unique identifier of the user.</param>
    /// <param name="email">User's email address.</param>
    /// <param name="role">User's role.</param>
    /// <param name="clientId">Identifier of the client requesting the token.</param>
    /// <returns>An object with the access token and refresh token.</returns>
    TokenResponseDto GenerateTokens(Guid userId, string email, string role, string? clientId);


    /// <summary>
    /// Checks if a token has been tampered with or expired, without throwing an exception.
    /// </summary>
    /// <param name="token">JWT token to verify.</param>
    /// <returns>True if the token is intact and valid, false otherwise.</returns>
    bool IsTokenValid(string token);
}
