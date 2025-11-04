using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SessionController> _logger;

    public SessionController(
        IUserSessionRepository sessionRepository,
        IUserRepository userRepository,
        ILogger<SessionController> logger)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active sessions grouped by user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserSessionsDto>>> GetSessions()
    {
        var sessionsByUser = await _sessionRepository.GetAllActiveSessionsGroupedByUserAsync();
        
        var result = new List<UserSessionsDto>();

        foreach (var (userId, sessions) in sessionsByUser)
        {
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for existing sessions", userId);
                continue;
            }

            var sessionDtos = sessions.Select(s => new SessionMonitorDto
            {
                SessionId = s.SessionId,
                ClientId = s.ClientId,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                LastActivity = s.LastActivity,
                Revoked = s.Revoked,
                RevokedAt = s.RevokedAt
            }).ToList();

            result.Add(new UserSessionsDto
            {
                UserId = user.Id,
                UserName = user.Username,
                Sessions = sessionDtos
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Revoke a specific session by sessionId
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> RevokeSession(string sessionId)
    {
        var session = await _sessionRepository.FindBySessionIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        // Get current user ID from JWT claims
        var currentUserId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        // If trying to revoke a session from the same user, check if it's the active session
        if (!string.IsNullOrEmpty(currentUserId) && session.UserId == currentUserId)
        {
            // Extract access token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var currentAccessToken = authHeader.Substring("Bearer ".Length).Trim();
                var currentAccessTokenHash = ComputeAccessTokenHash(currentAccessToken);

                // Check if this is the active session by comparing token hashes
                if (session.AccessTokenHash == currentAccessTokenHash)
                {
                    _logger.LogWarning("User {UserId} attempted to revoke their active session {SessionId}", currentUserId, sessionId);
                    return BadRequest(new { message = "No puedes revocar tu propia sesión activa" });
                }

                // Allow revoking other sessions belonging to the same user
                _logger.LogInformation("User {UserId} is revoking their own session {SessionId} from a different device", currentUserId, sessionId);
            }
        }

        session.Revoke();
        await _sessionRepository.UpdateAsync(session);

        return Ok(new { message = "Session revoked successfully" });
    }

    private static string ComputeAccessTokenHash(string accessToken)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(accessToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
