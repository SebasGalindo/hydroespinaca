using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.DTOs;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using BffService.Api.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace BffService.Api.Controllers;

[ApiController]
[Route("system")]
[AllowAnonymous] // We'll validate session manually
public class SystemStatusController : BaseAuthenticatedController
{
    private readonly ISessionService _sessionService;
    private readonly ISystemStatusService _systemStatusService;
    private readonly IFuzzyServiceClient _fuzzyServiceClient;
    private readonly IMemoryCache _cache;

    public SystemStatusController(
        ISessionService sessionService,
        ISystemStatusService systemStatusService,
        ISessionTokenService sessionTokenService,
        IWeatherService weatherService,
        IFuzzyServiceClient fuzzyServiceClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<SystemStatusController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _sessionService = sessionService;
        _systemStatusService = systemStatusService;
        _fuzzyServiceClient = fuzzyServiceClient;
        _cache = cache;
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
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await _systemStatusService.GetSystemStatusAsync(session.AccessToken, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var sessionId = GetSessionIdFromRequest();
            return ControllerExceptionHandler.HandleException(
                ex,
                Logger,
                "getting system status",
                sessionId,
                HttpContext);
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
        try
        {
            // Get session ID and validate session exists (we don't need the access token for fuzzy-service)
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new SessionNotFoundException("Session ID not found in request");
            }

            var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionId, cancellationToken);
            if (sessionInfo == null)
            {
                throw new SessionNotFoundException($"Session not found: {sessionId}");
            }

            // Try to get from cache first
            const string cacheKey = "fuzzy_rules_summary";
            if (_cache.TryGetValue(cacheKey, out List<FuzzyRuleSummaryDto>? cachedRules) && cachedRules != null)
            {
                Logger.LogDebug("Fuzzy rules retrieved from cache ({Count} rules)", cachedRules.Count);
                return Ok(cachedRules);
            }

            // Cache miss - fetch from fuzzy-service
            Logger.LogInformation("Cache miss for fuzzy rules - fetching from fuzzy-service");
            var rules = await _fuzzyServiceClient.GetRulesSummaryAsync(cancellationToken);

            // Cache the result for 24 hours
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(cacheKey, rules, cacheOptions);

            Logger.LogInformation("Fuzzy rules fetched and cached successfully ({Count} rules, 24h TTL)", rules.Count);
            return Ok(rules);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Error communicating with fuzzy-service");
            return StatusCode(503, new { message = "Fuzzy service unavailable" });
        }
        catch (Exception ex)
        {
            var sessionId = GetSessionIdFromRequest();
            return ControllerExceptionHandler.HandleException(
                ex,
                Logger,
                "getting fuzzy rules",
                sessionId,
                HttpContext);
        }
    }
}
