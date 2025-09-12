using Microsoft.AspNetCore.Mvc;
using BffService.Application.DTOs;
using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Configuration;

namespace BffService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISessionService sessionService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _sessionService = sessionService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _sessionService.LoginAsync(request, cancellationToken);
        
        // Set session ID in response header using config values
        var sessionIdHeader = _configuration[BffConstants.Sessions.SessionIdHeaderConfigKey] ?? "X-Session-Id";
        var csrfTokenHeader = _configuration[BffConstants.Sessions.CsrfTokenHeaderConfigKey] ?? "X-CSRF-Token";
        
        Response.Headers.Add(sessionIdHeader, result.SessionId);
        Response.Headers.Add(csrfTokenHeader, result.CsrfToken);
        
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _sessionService.LogoutAsync(request, cancellationToken);
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