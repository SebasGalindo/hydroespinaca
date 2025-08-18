using AuthService.Application.Features.Permissions.Commands.CreatePermission;
using AuthService.Application.Features.Permissions.Commands.DeletePermission;
using AuthService.Application.Features.Permissions.Commands.UpdatePermission;
using AuthService.Application.Features.Permissions.DTOs;
using AuthService.Application.Features.Permissions.Queries.GetAllPermissions;
using AuthService.Application.Features.Permissions.Queries.GetPermission;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<PermissionResponseDto>> Create([FromBody] CreatePermissionRequestDto request)
    {
        var command = new CreatePermissionCommand(request.Code, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "role_admin,role_user")]
    public async Task<ActionResult<PermissionResponseDto>> GetByCode(string code)
    {
        var query = new GetPermissionQuery(code);
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "role_admin,role_user")]
    public async Task<ActionResult<List<PermissionResponseDto>>> GetAll()
    {
        var query = new GetAllPermissionsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<PermissionResponseDto>> Update(string code, [FromBody] UpdatePermissionRequestDto request)
    {
        var command = new UpdatePermissionCommand(code, request.Name, request.Description);
        var result = await _mediator.Send(command);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult> Delete(string code)
    {
        var command = new DeletePermissionCommand(code);
        var result = await _mediator.Send(command);
        if (!result)
            return NotFound();

        return NoContent();
    }
}