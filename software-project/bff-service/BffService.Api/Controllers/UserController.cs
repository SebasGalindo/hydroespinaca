using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("users")]
[AllowAnonymous] // We'll validate session and role manually
public class UserController : BaseAuthenticatedController
{
    private readonly IAuthServiceClient _authServiceClient;

    public UserController(
        IAuthServiceClient authServiceClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<UserController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _authServiceClient = authServiceClient;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var users = await _authServiceClient.GetAllUsersAsync(session.AccessToken, cancellationToken);
            return Ok(users);
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
            Logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var user = await _authServiceClient.GetUserByIdAsync(id, session.AccessToken, cancellationToken);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
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
            Logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create([FromBody] UserCreateDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var user = await _authServiceClient.CreateUserAsync(request, session.AccessToken, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
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
            Logger.LogWarning(ex, "Error from auth service creating user");
            return BadRequest(new { message = "Failed to create user" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update existing user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var user = await _authServiceClient.UpdateUserAsync(id, request, session.AccessToken, cancellationToken);
            return Ok(user);
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
            Logger.LogWarning(ex, "Error from auth service updating user");
            return BadRequest(new { message = "Failed to update user" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _authServiceClient.DeleteUserAsync(id, session.AccessToken, cancellationToken);
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
            Logger.LogWarning(ex, "Error from auth service deleting user");
            return NotFound(new { message = "User not found" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
