using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Api.Controllers.Base;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("users")]
[AllowAnonymous] // We'll validate session and role manually
public class UserController : CrudControllerBase
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
        return await ExecuteAuthenticatedAsync(
            _authServiceClient.GetAllUsersAsync,
            "getting users",
            cancellationToken);
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
        return await ExecuteAuthenticatedWithIdAsync(
            id,
            _authServiceClient.GetUserByIdAsync,
            "getting user",
            cancellationToken,
            result => HandleNullResult(result, "User"));
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
        return await ExecuteAuthenticatedWithBodyAsync(
            request,
            _authServiceClient.CreateUserAsync,
            "creating user",
            cancellationToken,
            result => CreatedResult(nameof(GetById), new { id = result.Id }, result));
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
        return await ExecuteAuthenticatedWithIdAndBodyAsync(
            id,
            request,
            _authServiceClient.UpdateUserAsync,
            "updating user",
            cancellationToken);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);

            // Prevent self-deletion
            if (session.UserId == id)
            {
                Logger.LogWarning("User {UserId} attempted to delete themselves", id);
                return BadRequest(new { message = "No puedes eliminar tu propia cuenta de usuario" });
            }

            await _authServiceClient.DeleteUserAsync(id, session.AccessToken, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BffService.Api.Helpers.ControllerExceptionHandler.HandleException(
                ex,
                Logger,
                "deleting user",
                id,
                HttpContext);
        }
    }
}
