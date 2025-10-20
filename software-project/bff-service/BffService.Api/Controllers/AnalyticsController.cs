using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Analytics;
using Microsoft.Extensions.Configuration;

namespace BffService.Api.Controllers;

[ApiController]
[Route("analytics")]
[AllowAnonymous] // We'll validate session manually
public class AnalyticsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        ISessionService sessionService,
        IAnalyticsService analyticsService,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<AnalyticsController> logger)
    {
        _sessionService = sessionService;
        _analyticsService = analyticsService;
        _sessionTokenService = sessionTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets environmental aggregates with date range and granularity filtering
    /// </summary>
    [HttpPost("environmental")]
    [ProducesResponseType(typeof(List<EnvironmentalAggregateResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetEnvironmentalAggregates(
        [FromBody] EnvironmentalAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        string? sessionId = null;
        try
        {
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
                _logger.LogWarning("Session ID not found in request (GetEnvironmentalAggregates)");
                return Unauthorized(new { message = "Session ID not found" });
            }

            _logger.LogDebug(
                "Processing GetEnvironmentalAggregates for session {SessionId} - will validate and refresh tokens if needed",
                sessionId
            );

            // 3️⃣ Ensure we have a valid access token (refresh if needed)
            // NOTE: GetSessionWithValidTokensAsync handles token expiration and automatic refresh
            var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);

            // 4️⃣ Call the analytics service
            var result = await _analyticsService.GetEnvironmentalAggregatesAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(
                ex,
                "Session expired and cannot be refreshed: {SessionId}",
                sessionId
            );
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid token exception for session {SessionId} - this should NOT happen if auto-refresh works correctly",
                sessionId
            );
            return Unauthorized(new { message = "Session expired" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new { message = ex.Message });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout");
            return StatusCode(504, new { message = "Request timeout - services not responding" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environmental aggregates");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets actuator analytics with date range and granularity filtering
    /// </summary>
    [HttpPost("actuators")]
    [ProducesResponseType(typeof(ActuatorAnalyticsResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetActuatorAnalytics(
        [FromBody] ActuatorAnalyticsRequest request,
        CancellationToken cancellationToken)
    {
        string? sessionId = null;
        try
        {
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
                _logger.LogWarning("Session ID not found in request (GetActuatorAnalytics)");
                return Unauthorized(new { message = "Session ID not found" });
            }

            _logger.LogDebug(
                "Processing GetActuatorAnalytics for session {SessionId} - will validate and refresh tokens if needed",
                sessionId
            );

            // 3️⃣ Ensure we have a valid access token (refresh if needed)
            // NOTE: GetSessionWithValidTokensAsync handles token expiration and automatic refresh
            var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);

            // 4️⃣ Call the analytics service
            var result = await _analyticsService.GetActuatorAnalyticsAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            _logger.LogWarning(
                ex,
                "Session expired and cannot be refreshed: {SessionId}",
                sessionId
            );
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid token exception for session {SessionId} - this should NOT happen if auto-refresh works correctly",
                sessionId
            );
            return Unauthorized(new { message = "Session expired" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new { message = ex.Message });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout");
            return StatusCode(504, new { message = "Request timeout - services not responding" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting actuator analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
