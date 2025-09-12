using Microsoft.AspNetCore.Mvc;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.Constants;
using BffService.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace BffService.Api.Controllers;

[ApiController]
[Route("proxy")]
public class ProxyController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IProxyService _proxyService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        ISessionService sessionService, 
        IProxyService proxyService,
        IAuthService authService,
        IConfiguration configuration,
        ILogger<ProxyController> logger)
    {
        _sessionService = sessionService;
        _proxyService = proxyService;
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("{**path}")]
    public async Task<ActionResult> ProxyGet(string path, CancellationToken cancellationToken)
    {
        return await ForwardRequest("GET", path, cancellationToken);
    }

    [HttpPost("{**path}")]
    public async Task<ActionResult> ProxyPost(string path, CancellationToken cancellationToken)
    {
        return await ForwardRequest("POST", path, cancellationToken);
    }

    [HttpPut("{**path}")]
    public async Task<ActionResult> ProxyPut(string path, CancellationToken cancellationToken)
    {
        return await ForwardRequest("PUT", path, cancellationToken);
    }

    [HttpDelete("{**path}")]
    public async Task<ActionResult> ProxyDelete(string path, CancellationToken cancellationToken)
    {
        return await ForwardRequest("DELETE", path, cancellationToken);
    }

    [HttpPatch("{**path}")]
    public async Task<ActionResult> ProxyPatch(string path, CancellationToken cancellationToken)
    {
        return await ForwardRequest("PATCH", path, cancellationToken);
    }

    private async Task<ActionResult> ForwardRequest(string method, string path, CancellationToken cancellationToken)
    {
        // Get session ID from headers using config
        var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";
        var csrfTokenHeader = _configuration[BffConstants.Sessions.CsrfTokenHeaderConfigKey] ?? "X-CSRF-Token";

        if (!Request.Headers.TryGetValue(sessionIdHeader, out var sessionIdValues) ||
            !sessionIdValues.Any())
        {
            return Unauthorized(new { message = "Session ID required" });
        }

        var sessionId = sessionIdValues.First()!;
        
        // Validate CSRF token for state-changing operations
        if (IsStateChangingOperation(method))
        {
            if (!Request.Headers.TryGetValue(csrfTokenHeader, out var csrfValues) ||
                !csrfValues.Any())
            {
                return BadRequest(new { message = "CSRF token required" });
            }
        }

        // Get session info and validate
        var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionId, cancellationToken);
        if (sessionInfo == null || !sessionInfo.IsValid)
        {
            return Unauthorized(new { message = "Invalid or expired session" });
        }

        // Construct full path for proxy
        var fullPath = $"/{path}";
        
        if (!_proxyService.IsValidProxyPath(fullPath))
        {
            return BadRequest(new { message = "Invalid proxy path" });
        }

        var targetService = _proxyService.GetTargetService(fullPath);

        // Read request body if present
        string? requestBody = null;
        if (Request.ContentLength > 0)
        {
            using var reader = new StreamReader(Request.Body);
            requestBody = await reader.ReadToEndAsync(cancellationToken);
        }

        // Build headers dictionary
        var headers = Request.Headers
            .Where(h => !IsSystemHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var proxyRequest = new ProxyRequest(
            method,
            RemoveProxyPrefix(fullPath),
            headers,
            requestBody
        );

        // Forward request
        var session = await GetSessionWithTokens(sessionId, cancellationToken);
        if (session == null)
        {
            return Unauthorized(new { message = "Session not found" });
        }

        var proxyResponse = await _proxyService.ForwardRequestAsync(
            proxyRequest,
            session.AccessToken,
            targetService,
            cancellationToken);

        // Set response headers
        foreach (var header in proxyResponse.Headers)
        {
            Response.Headers.TryAdd(header.Key, header.Value);
        }

        return new ContentResult
        {
            StatusCode = proxyResponse.StatusCode,
            Content = proxyResponse.Body,
            ContentType = proxyResponse.Headers.GetValueOrDefault("Content-Type", "application/json")
        };
    }

    private static bool IsStateChangingOperation(string method)
    {
        return method.ToUpperInvariant() is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private static bool IsSystemHeader(string headerName)
    {
        var systemHeaders = new[]
        {
            "host",
            "connection",
            "upgrade",
            "expect",
            "te",
            "trailer",
            "transfer-encoding",
            "content-length"
        };

        return systemHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static string RemoveProxyPrefix(string path)
    {
        if (path.StartsWith(BffConstants.Proxy.ProxyBasePath))
        {
            return path.Substring(BffConstants.Proxy.ProxyBasePath.Length);
        }
        
        return path;
    }

    private async Task<Domain.Entities.Session?> GetSessionWithTokens(string sessionId, CancellationToken cancellationToken)
    {
        // Get the full session with all token information
        var session = await _sessionService.GetFullSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            throw new SessionNotFoundException(sessionId);
        }
        
        if (!session.HasValidTokens())
        {
            _logger.LogWarning("Session has invalid tokens: {SessionId}", sessionId);
            throw new InvalidTokenException("Session tokens are invalid");
        }

        // Check if the access token is expired and needs refreshing
        if (session.IsExpired() && session.CanRefresh())
        {
            _logger.LogInformation("Access token expired for session {SessionId}, attempting refresh", sessionId);
            
            // Use the auth service to refresh the token
            var newTokenInfo = await _authService.RefreshTokenAsync(session.RefreshToken, cancellationToken);
            
            // Update the session with the new tokens
            session.UpdateAccessToken(newTokenInfo.AccessToken, newTokenInfo.ExpiresAt);
            
            // If we got a new refresh token, update that too
            if (!string.IsNullOrEmpty(newTokenInfo.RefreshToken))
            {
                session.SetTokens(
                    newTokenInfo.AccessToken, 
                    newTokenInfo.RefreshToken, 
                    newTokenInfo.ExpiresAt, 
                    newTokenInfo.RefreshTokenExpiresAt
                );
            }
            
            // Save the updated session
            await _sessionService.UpdateSessionAsync(session, cancellationToken);
            
            _logger.LogInformation("Successfully refreshed tokens for session: {SessionId}", sessionId);
        }
        else if (session.IsExpired())
        {
            _logger.LogWarning("Session {SessionId} is expired and cannot be refreshed", sessionId);
            throw new SessionExpiredException(sessionId);
        }

        return session;
    }
}