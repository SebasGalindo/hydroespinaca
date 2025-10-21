using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("roles")]
[AllowAnonymous] // We'll validate session manually
public class RoleController : BaseAuthenticatedController
{
    private readonly IAuthServiceClient _authServiceClient;

    public RoleController(
        IAuthServiceClient authServiceClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<RoleController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _authServiceClient = authServiceClient;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var roles = await _authServiceClient.GetAllRolesAsync(session.AccessToken, cancellationToken);
            return Ok(roles);
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
            Logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get role by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(RoleResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var role = await _authServiceClient.GetRoleByCodeAsync(code, session.AccessToken, cancellationToken);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            return Ok(role);
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
            Logger.LogError(ex, "Error getting role {RoleCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var role = await _authServiceClient.CreateRoleAsync(request, session.AccessToken, cancellationToken);
            return CreatedAtAction(nameof(GetByCode), new { code = role.Code }, role);
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
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service creating role");
            return BadRequest(new { message = "Failed to create role" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating role");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update existing role
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(typeof(RoleResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(string code, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var role = await _authServiceClient.UpdateRoleAsync(code, request, session.AccessToken, cancellationToken);
            return Ok(role);
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
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service updating role");
            return BadRequest(new { message = "Failed to update role" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating role {RoleCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _authServiceClient.DeleteRoleAsync(code, session.AccessToken, cancellationToken);
            return NoContent();
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
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service deleting role");
            return NotFound(new { message = "Role not found" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting role {RoleCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
