using AuthService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;

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
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions");
            return StatusCode(500, new { message = "Error retrieving sessions" });
        }
    }

    /// <summary>
    /// Revoke a specific session by sessionId
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> RevokeSession(string sessionId)
    {
        try
        {
            var session = await _sessionRepository.FindBySessionIdAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            session.Revoke();
            await _sessionRepository.UpdateAsync(session);

            return Ok(new { message = "Session revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Error revoking session" });
        }
    }
}
