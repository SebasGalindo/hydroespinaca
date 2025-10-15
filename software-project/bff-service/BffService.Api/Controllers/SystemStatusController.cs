using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BffService.Api.Controllers;

[ApiController]
[Route("system")]
[AllowAnonymous] // We’ll validate session manually
public class SystemStatusController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ISystemStatusService _systemStatusService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemStatusController> _logger;

    public SystemStatusController(
        ISessionService sessionService,
        ISystemStatusService systemStatusService,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<SystemStatusController> logger)
    {
        _sessionService = sessionService;
        _systemStatusService = systemStatusService;
        _sessionTokenService = sessionTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets consolidated system status including sensor readings and actuator jobs
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSystemStatus(CancellationToken cancellationToken)
    {
        try
        {
            string? sessionId = null;
            var sessionIdHeaderKey = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";

            // 1️⃣ Try to read from cookie (web)
            if (Request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
            {
                sessionId = cookieSessionId;
            }
            // 2️⃣ Fallback to header (mobile or external clients)
            else if (Request.Headers.TryGetValue(sessionIdHeaderKey, out var headerSessionId))
            {
                sessionId = headerSessionId.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Unauthorized(new { message = "Session ID not found" });
            }

            // 3️⃣ Validate session
            var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionId, cancellationToken);
            if (sessionInfo == null || !sessionInfo.IsValid)
            {
                return Unauthorized(new { message = "Invalid or expired session" });
            }

            // 4️⃣ Ensure we have a valid access token (refresh if needed)
            var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);

            // 5️⃣ Call the actual system status service
            var result = await _systemStatusService.GetSystemStatusAsync(session.AccessToken, cancellationToken);

            return Ok(result);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout");
            return StatusCode(504, new { message = "Request timeout - services not responding" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
