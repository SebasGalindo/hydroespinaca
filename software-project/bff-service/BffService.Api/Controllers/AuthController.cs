using Microsoft.AspNetCore.Mvc;
using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;
using HydroEspinaca.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.DTOs;

namespace BffService.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        ISessionService sessionService,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _sessionService = sessionService;
        _sessionTokenService = sessionTokenService;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Ensures Secure flag is true in production for cookie security.
    /// SonarQube rule S2092: Cookies should be "secure" in production deployments.
    /// </summary>
    private bool GetSecureCookieSetting()
    {
        // In production, always use Secure cookies (HTTPS only)
        if (_environment.IsProduction())
        {
            return true;
        }
        // In development, allow configuration override for local testing
        return _configuration.GetValue<bool>("Cookies:Secure", true);
    }

    [AllowAnonymous]
    [HttpPost("login/web")]
    public async Task<ActionResult<WebLoginResponseDto>> LoginWeb([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        // Capture client information from the current HTTP context
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Enrich the request with client information and web clientId
        var enrichedRequest = new LoginRequestDto(
            request.Email,
            request.Password,
            ClientIdentifiers.WebApp,
            request.SessionId,
            ipAddress,
            userAgent,
            request.CsrfToken
        );

        var result = await _sessionService.LoginAsync(enrichedRequest, cancellationToken);

        // Set cookies for web clients with SessionId in HttpOnly cookie
        var sessionCookieOptions = new CookieOptions
        {
            HttpOnly = true, // SessionId must be HttpOnly for security
            Secure = GetSecureCookieSetting(), // Ensures Secure=true in production (S2092)
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("Cookies:SameSite", "Strict")),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>("Sessions:RefreshTokenExpiryDays", 7))
        };

        var csrfCookieOptions = new CookieOptions
        {
            HttpOnly = false, // CSRF token needs to be accessible by JavaScript
            Secure = GetSecureCookieSetting(), // Ensures Secure=true in production (S2092)
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("Cookies:SameSite", "Strict")),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>("Sessions:RefreshTokenExpiryDays", 7))
        };        // Only set domain if configured
        var domain = _configuration.GetValue<string>("Cookies:Domain");
        if (!string.IsNullOrEmpty(domain))
        {
            sessionCookieOptions.Domain = domain;
            csrfCookieOptions.Domain = domain;
        }

        // Set secure cookies with different HttpOnly settings
        Response.Cookies.Append("SessionId", result.SessionId, sessionCookieOptions);
        Response.Cookies.Append("CsrfToken", result.CsrfToken, csrfCookieOptions);

        return Ok(); // sin body - solo cookies
    }

    [AllowAnonymous]
    [HttpPost("login/mobile")]
    public async Task<ActionResult<MobileLoginResponseDto>> LoginMobile([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        // Capture client information from the current HTTP context
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Enrich the request with client information and mobile clientId
        var enrichedRequest = new LoginRequestDto(
            request.Email,
            request.Password,
            ClientIdentifiers.MobileApp,
            request.SessionId,
            ipAddress,
            userAgent,
            request.CsrfToken
        );

        var result = await _sessionService.LoginAsync(enrichedRequest, cancellationToken);

        // Set headers for mobile clients
        var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";
        var csrfTokenHeader = _configuration[BffConstants.Sessions.CsrfTokenHeaderConfigKey] ?? "X-CSRF-Token";

        Response.Headers[sessionIdHeader] = result.SessionId;
        Response.Headers[csrfTokenHeader] = result.CsrfToken;

        // Return session data in response body for mobile secure storage
        return Ok(new MobileLoginResponseDto(result.SessionId, result.CsrfToken));
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        string? sessionId = null;

        // For web: try to read SessionId from HttpOnly cookie first
        if (Request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
        {
            sessionId = cookieSessionId;
        }
        // For mobile: try to read from request body if no cookie
        else
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(body) && body != "{}")
                {
                    var requestData = System.Text.Json.JsonSerializer.Deserialize<LogoutRequestDto>(body);
                    sessionId = requestData?.SessionId;
                }
            }
            catch
            {
                // Ignore JSON parsing errors for empty/invalid bodies
            }
        }

        // Try to logout from session repository if we have a sessionId
        if (!string.IsNullOrEmpty(sessionId))
        {
            var logoutRequest = new LogoutRequestDto(sessionId);
            try
            {
                await _sessionService.LogoutAsync(logoutRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during logout for session {SessionId}, will still clear cookies", sessionId);
            }
        }

        // ALWAYS clear cookies for web clients, even if there's no active session
        // This handles cases where cookies persist after session was deleted (e.g., server restart, session limit)
        var sessionCookieOptions = new CookieOptions
        {
            HttpOnly = true, // SessionId is HttpOnly
            Secure = GetSecureCookieSetting(), // Ensures Secure=true in production (S2092)
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("Cookies:SameSite", "Strict")),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire in the past to delete
        };

        var csrfCookieOptions = new CookieOptions
        {
            HttpOnly = false, // CsrfToken is NOT HttpOnly
            Secure = GetSecureCookieSetting(), // Ensures Secure=true in production (S2092)
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("Cookies:SameSite", "Strict")),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire in the past to delete
        };

        // Only set domain if configured (must match creation)
        var domain = _configuration.GetValue<string>("Cookies:Domain");
        if (!string.IsNullOrEmpty(domain))
        {
            sessionCookieOptions.Domain = domain;
            csrfCookieOptions.Domain = domain;
        }

        // Always delete both cookies (SessionId is HttpOnly, CsrfToken is not)
        Response.Cookies.Append("SessionId", "", sessionCookieOptions);
        Response.Cookies.Append("CsrfToken", "", csrfCookieOptions);

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var refreshed = await _sessionService.RefreshTokenAsync(request, cancellationToken);
        
        if (refreshed)
        {
            return Ok(new { message = "Token refreshed successfully" });
        }
        
        return Unauthorized(new { message = "Unable to refresh token" });
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetCurrentSession(CancellationToken cancellationToken)
    {
        try
        {
            string? sessionId = null;

            // For web: try to read SessionId from HttpOnly cookie first
            if (Request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
            {
                sessionId = cookieSessionId;
            }
            // For mobile: try to read from headers
            else
            {
                var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";
                if (Request.Headers.TryGetValue(sessionIdHeader, out var headerSessionId))
                {
                    sessionId = headerSessionId.FirstOrDefault();
                }
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized(new { message = "No active session" });
            }

            // Validate session and auto-refresh tokens if needed
            var session = await _sessionTokenService.GetSessionWithValidTokensAsync(sessionId, cancellationToken);

            // Format role: remove "role_" prefix if present and capitalize
            var formattedRole = FormatRole(session.UserRole ?? "user");

            // Return user information for frontend
            return Ok(new UserSessionDto(
                session.UserId ?? "",
                session.SessionId,
                session.Username ?? "Usuario",
                session.Email ?? "",
                formattedRole
            ));
        }
        catch (Exception ex)
        {
            var sessionId = GetSessionIdFromRequest();
            return BffService.Api.Helpers.ControllerExceptionHandler.HandleException(
                ex,
                _logger,
                "checking session",
                sessionId,
                HttpContext);
        }
    }

    private string? GetSessionIdFromRequest()
    {
        if (Request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
        {
            return cookieSessionId;
        }

        var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";
        if (Request.Headers.TryGetValue(sessionIdHeader, out var headerSessionId))
        {
            return headerSessionId.FirstOrDefault();
        }

        return null;
    }

    private static string FormatRole(string role)
    {
        if (string.IsNullOrEmpty(role))
            return "Usuario";

        // Remove "role_" prefix if present
        var cleanRole = role.StartsWith("role_", StringComparison.OrdinalIgnoreCase)
            ? role.Substring(5)
            : role;

        // Capitalize first letter
        if (cleanRole.Length > 0)
        {
            cleanRole = char.ToUpper(cleanRole[0]) + cleanRole.Substring(1).ToLower();
        }

        // Map specific roles to Spanish
        return cleanRole.ToLower() switch
        {
            "admin" => "Administrador",
            "user" => "Usuario",
            _ => cleanRole
        };
    }

    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<SessionInfoDto>> GetSessionInfo(string sessionId, CancellationToken cancellationToken)
    {
        var sessionInfo = await _sessionService.GetSessionInfoAsync(sessionId, cancellationToken);
        
        if (sessionInfo == null)
        {
            return NotFound(new { message = "Session not found" });
        }
        
        return Ok(sessionInfo);
    }
}