using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly IUserSessionService _sessionService;
    private readonly IUserSessionRepository _sessionRepository;

    public SessionController(
        IUserSessionService sessionService,
        IUserSessionRepository sessionRepository)
    {
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
    }

    /// <summary>
    /// Get all active sessions for the authenticated user
    /// </summary>
    [Authorize]
    [HttpGet("active")]
    public async Task<ActionResult> GetActiveSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await _sessionService.GetActiveUserSessionsAsync(userId);
        var sessionDtos = sessions.Select(s => new
        {
            s.SessionId,
            s.ClientId,
            s.CreatedAt,
            s.LastActivity,
            s.ExpiresAt,
            s.IpAddress,
            s.UserAgent,
            IsActive = s.IsActive
        });

        return Ok(sessionDtos);
    }

    /// <summary>
    /// Get all sessions (active and revoked) for the authenticated user
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult> GetAllSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sessions = await _sessionRepository.FindAllByUserIdAsync(userId);
        var sessionDtos = sessions.Select(s => new
        {
            s.SessionId,
            s.ClientId,
            s.CreatedAt,
            s.LastActivity,
            s.ExpiresAt,
            s.Revoked,
            s.RevokedAt,
            s.IpAddress,
            s.UserAgent,
            IsActive = s.IsActive
        });

        return Ok(sessionDtos);
    }

    /// <summary>
    /// Revoke a specific session by sessionId
    /// </summary>
    [Authorize]
    [HttpPost("{sessionId}/revoke")]
    public async Task<ActionResult> RevokeSession(string sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify the session belongs to the user
        var session = await _sessionRepository.FindBySessionIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        if (session.UserId != userId)
        {
            return Forbid();
        }

        await _sessionService.RevokeSessionAsync(sessionId);
        return Ok(new { message = "Session revoked successfully" });
    }

    /// <summary>
    /// Revoke all sessions for the authenticated user
    /// </summary>
    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<ActionResult> RevokeAllSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _sessionService.RevokeAllUserSessionsAsync(userId);
        return Ok(new { message = "All sessions revoked successfully" });
    }

    /// <summary>
    /// Admin endpoint: Get all sessions for a specific user by userId
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult> GetUserSessions(string userId)
    {
        var sessions = await _sessionRepository.FindAllByUserIdAsync(userId);
        var sessionDtos = sessions.Select(s => new
        {
            s.SessionId,
            s.UserId,
            s.ClientId,
            s.CreatedAt,
            s.LastActivity,
            s.ExpiresAt,
            s.Revoked,
            s.RevokedAt,
            s.IpAddress,
            s.UserAgent,
            IsActive = s.IsActive
        });

        return Ok(sessionDtos);
    }

    /// <summary>
    /// Admin endpoint: Revoke a specific session by sessionId
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpPost("admin/{sessionId}/revoke")]
    public async Task<ActionResult> AdminRevokeSession(string sessionId)
    {
        var session = await _sessionRepository.FindBySessionIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        await _sessionService.RevokeSessionAsync(sessionId);
        return Ok(new { message = "Session revoked successfully" });
    }

    /// <summary>
    /// Admin endpoint: Revoke all sessions for a specific user
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpPost("admin/user/{userId}/revoke-all")]
    public async Task<ActionResult> AdminRevokeAllUserSessions(string userId)
    {
        await _sessionService.RevokeAllUserSessionsAsync(userId);
        return Ok(new { message = $"All sessions for user {userId} revoked successfully" });
    }
}
