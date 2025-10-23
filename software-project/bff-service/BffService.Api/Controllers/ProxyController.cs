using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Domain.Interfaces;
using BffService.Domain.ValueObjects;
using BffService.Domain.Constants;
using BffService.Domain.Exceptions;
using BffService.Application.Services;

namespace BffService.Api.Controllers;

[ApiController]
[Route("proxy")]
[AllowAnonymous] // Allow anonymous access, we'll check authentication internally based on route
public class ProxyController : ControllerBase
{
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IProxyService _proxyService;
    private readonly IConfiguration _configuration;
    private readonly ICsrfValidationService _csrfValidationService;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        ISessionTokenService sessionTokenService,
        IProxyService proxyService,
        IConfiguration configuration,
        ICsrfValidationService csrfValidationService,
        ILogger<ProxyController> logger)
    {
        _sessionTokenService = sessionTokenService;
        _proxyService = proxyService;
        _configuration = configuration;
        _csrfValidationService = csrfValidationService;
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
        // URL decode the path to handle encoded slashes from Swagger UI
        var decodedPath = Uri.UnescapeDataString(path);
        
        // Construct full path for proxy (add /proxy prefix since route captures everything after proxy/)
        var fullPath = $"/proxy/{decodedPath}";
        
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

        string? accessToken = null;

        // Try to get access token from session if available
        var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";

        if (Request.Headers.TryGetValue(sessionIdHeader, out var sessionIdValues) && sessionIdValues.Any())
        {
            var sessionId = sessionIdValues.First()!;

            // Validate CSRF token for state-changing operations
            if (_csrfValidationService.IsStateChangingOperation(method))
            {
                if (!_csrfValidationService.ValidateCsrfToken(HttpContext))
                {
                    return BadRequest(new { message = "CSRF validation failed" });
                }
            }

            // Try to get session with valid tokens (auto-refresh if needed)
            try
            {
                var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);
                accessToken = session.AccessToken;
            }
            catch (SessionNotFoundException ex)
            {
                _logger.LogWarning(ex, "Session not found for session {SessionId}, continuing without token", sessionId);
            }
            catch (SessionExpiredException ex)
            {
                _logger.LogWarning(ex, "Session expired for session {SessionId}, continuing without token", sessionId);
            }
            catch (InvalidTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid tokens for session {SessionId}, continuing without token", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get session tokens for session {SessionId}, continuing without token", sessionId);
            }
        }

        // Forward request - let target service decide if authentication is required
        var proxyResponse = await _proxyService.ForwardRequestAsync(
            proxyRequest,
            accessToken,
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
        if (!path.StartsWith(BffConstants.Proxy.ProxyBasePath))
        {
            return path;
        }

        // Remove "/proxy" prefix first
        var pathWithoutProxy = path.Substring(BffConstants.Proxy.ProxyBasePath.Length);
        
        // Now remove the service prefix (e.g., "/auth", "/sensor", "/actuator")
        foreach (var serviceRoute in BffConstants.Proxy.ServiceRoutes.Keys)
        {
            if (pathWithoutProxy.StartsWith(serviceRoute, StringComparison.OrdinalIgnoreCase))
            {
                return pathWithoutProxy.Substring(serviceRoute.Length);
            }
        }
        
        return pathWithoutProxy;
    }

}