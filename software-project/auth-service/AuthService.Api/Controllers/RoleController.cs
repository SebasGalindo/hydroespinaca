using AuthService.Application.Features.Roles.Commands.CreateRole;
using AuthService.Application.Features.Roles.Commands.DeleteRole;
using AuthService.Application.Features.Roles.Commands.UpdateRole;
using AuthService.Application.Features.Roles.DTOs;
using AuthService.Application.Features.Roles.Queries.GetAllRoles;
using AuthService.Application.Features.Roles.Queries.GetRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/roles")]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<RoleResponseDto>> Create([FromBody] CreateRoleRequestDto request)
    {
        var command = new CreateRoleCommand(request.Code, request.Name, request.PermissionCodes);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "role_admin,role_user")]
    public async Task<ActionResult<RoleResponseDto>> GetByCode(string code)
    {
        var query = new GetRoleQuery(code);
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "role_admin,role_user")]
    public async Task<ActionResult<List<RoleResponseDto>>> GetAll()
    {
        var query = new GetAllRolesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<RoleResponseDto>> Update(string code, [FromBody] UpdateRoleRequestDto request)
    {
        var command = new UpdateRoleCommand(code, request.Name, request.PermissionCodes);
        var result = await _mediator.Send(command);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult> Delete(string code)
    {
        var command = new DeleteRoleCommand(code);
        var result = await _mediator.Send(command);
        if (!result)
            return NotFound();

        return NoContent();
    }
}