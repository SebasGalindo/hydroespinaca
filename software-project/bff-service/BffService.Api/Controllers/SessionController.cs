using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("sessions")]
[AllowAnonymous] // We'll validate session manually
public class SessionController : BaseAuthenticatedController
{
    private readonly IAuthServiceClient _authServiceClient;

    public SessionController(
        IAuthServiceClient authServiceClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<SessionController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _authServiceClient = authServiceClient;
    }

    /// <summary>
    /// Get all active sessions grouped by user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserSessionsDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var sessions = await _authServiceClient.GetAllSessionsAsync(session.AccessToken, cancellationToken);
            return Ok(sessions);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke a specific session by sessionId
    /// </summary>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RevokeSession(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);

            // Prevent revoking own active session
            if (session.SessionId == sessionId)
            {
                Logger.LogWarning("User with session {SessionId} attempted to revoke their own active session", sessionId);
                return BadRequest(new { message = "No puedes revocar tu propia sesión activa" });
            }

            await _authServiceClient.RevokeSessionAsync(sessionId, session.AccessToken, cancellationToken);
            return Ok(new { message = "Session revoked successfully" });
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (ServiceException ex) when (ex.StatusCode == 400)
        {
            Logger.LogWarning(ex, "Bad request from auth service revoking session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (ServiceException ex) when (ex.StatusCode == 404)
        {
            Logger.LogWarning(ex, "Session {SessionId} not found in auth service", sessionId);
            return NotFound(new { message = "Session not found or already revoked" });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service revoking session");
            return NotFound(new { message = "Session not found or already revoked" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
