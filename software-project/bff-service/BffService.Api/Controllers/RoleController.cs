using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Api.Controllers.Base;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("roles")]
[AllowAnonymous] // We'll validate session manually
public class RoleController : CrudControllerBase
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
        return await ExecuteAuthenticatedAsync(
            _authServiceClient.GetAllRolesAsync,
            "getting roles",
            cancellationToken);
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
        return await ExecuteAuthenticatedWithIdAsync(
            code,
            _authServiceClient.GetRoleByCodeAsync,
            "getting role",
            cancellationToken,
            result => HandleNullResult(result, "Role"));
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
        return await ExecuteAuthenticatedWithBodyAsync(
            request,
            _authServiceClient.CreateRoleAsync,
            "creating role",
            cancellationToken,
            result => CreatedResult(nameof(GetByCode), new { code = result.Code }, result));
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
        return await ExecuteAuthenticatedWithIdAndBodyAsync(
            code,
            request,
            _authServiceClient.UpdateRoleAsync,
            "updating role",
            cancellationToken);
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
        return await ExecuteAuthenticatedDeleteAsync(
            code,
            _authServiceClient.DeleteRoleAsync,
            "deleting role",
            cancellationToken);
    }
}
