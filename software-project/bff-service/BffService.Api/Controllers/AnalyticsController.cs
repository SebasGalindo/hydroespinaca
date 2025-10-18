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

            // 5️⃣ Call the analytics service
            var result = await _analyticsService.GetEnvironmentalAggregatesAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
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

            // 5️⃣ Call the analytics service
            var result = await _analyticsService.GetActuatorAnalyticsAsync(
                request,
                session.AccessToken,
                cancellationToken);

            return Ok(result);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
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
