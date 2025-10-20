using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.DTOs;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace BffService.Api.Controllers;

[ApiController]
[Route("system")]
[AllowAnonymous] // We’ll validate session manually
public class SystemStatusController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ISystemStatusService _systemStatusService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IWeatherService _weatherService;
    private readonly IFuzzyServiceClient _fuzzyServiceClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemStatusController> _logger;

    public SystemStatusController(
        ISessionService sessionService,
        ISystemStatusService systemStatusService,
        ISessionTokenService sessionTokenService,
        IWeatherService weatherService,
        IFuzzyServiceClient fuzzyServiceClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<SystemStatusController> logger)
    {
        _sessionService = sessionService;
        _systemStatusService = systemStatusService;
        _sessionTokenService = sessionTokenService;
        _weatherService = weatherService;
        _fuzzyServiceClient = fuzzyServiceClient;
        _cache = cache;
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
                _logger.LogWarning("Session ID not found in request (GetSystemStatus)");
                return Unauthorized(new { message = "Session ID not found" });
            }

            _logger.LogDebug(
                "Processing GetSystemStatus for session {SessionId} - will validate and refresh tokens if needed",
                sessionId
            );

            // 3️⃣ Ensure we have a valid access token (refresh if needed)
            // NOTE: GetSessionWithValidTokensAsync handles token expiration and automatic refresh
            var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);

            // 4️⃣ Call the actual system status service
            var result = await _systemStatusService.GetSystemStatusAsync(session.AccessToken, cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(
                ex,
                "⚠️  MICROSERVICE AUTHORIZATION FAILED: One or more microservices rejected the JWT token (403 Forbidden) | SessionId: {SessionId} | This indicates a JWT configuration problem in the microservices",
                sessionId
            );
            return StatusCode(503, new
            {
                message = "System status unavailable - microservices are not accepting authentication tokens",
                error = "SERVICE_UNAVAILABLE",
                details = "The downstream microservices are rejecting JWT tokens. This is a configuration issue that needs to be resolved in the microservices.",
                retryAfter = 60 // Suggest client to retry after 60 seconds
            });
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

    /// <summary>
    /// Gets a summary list of all fuzzy logic rules with their names and descriptions.
    /// Results are cached for 24 hours to optimize performance.
    /// </summary>
    [HttpGet("fuzzy-rules")]
    [ProducesResponseType(typeof(List<FuzzyRuleSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetFuzzyRules(CancellationToken cancellationToken)
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
                _logger.LogWarning("Session ID not found in request (GetFuzzyRules)");
                return Unauthorized(new { message = "Session ID not found" });
            }

            _logger.LogDebug("Processing GetFuzzyRules for session {SessionId}", sessionId);

            // 2️⃣ Validate session exists (we don't need the access token for fuzzy-service)
            var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionId, cancellationToken);
            if (sessionInfo == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", sessionId);
                return Unauthorized(new { message = "Session not found" });
            }

            // 3️⃣ Try to get from cache first
            const string cacheKey = "fuzzy_rules_summary";
            if (_cache.TryGetValue(cacheKey, out List<FuzzyRuleSummaryDto>? cachedRules) && cachedRules != null)
            {
                _logger.LogDebug("Fuzzy rules retrieved from cache ({Count} rules)", cachedRules.Count);
                return Ok(cachedRules);
            }

            // 4️⃣ Cache miss - fetch from fuzzy-service
            _logger.LogInformation("Cache miss for fuzzy rules - fetching from fuzzy-service");
            var rules = await _fuzzyServiceClient.GetRulesSummaryAsync(cancellationToken);

            // 5️⃣ Cache the result for 24 hours
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(cacheKey, rules, cacheOptions);

            _logger.LogInformation("Fuzzy rules fetched and cached successfully ({Count} rules, 24h TTL)", rules.Count);
            return Ok(rules);
        }
        catch (SessionNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
            return Unauthorized(new { message = "Session not found" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error communicating with fuzzy-service");
            return StatusCode(503, new { message = "Fuzzy service unavailable" });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout");
            return StatusCode(504, new { message = "Request timeout - fuzzy service not responding" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fuzzy rules");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
