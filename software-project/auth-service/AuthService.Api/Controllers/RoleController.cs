using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/roles")]
public class RoleController : ControllerBase
{
    private readonly ICreateRoleUseCase _createRole;
    private readonly IGetRoleUseCase _getRole;
    private readonly IGetAllRolesUseCase _getAllRoles;
    private readonly IUpdateRoleUseCase _updateRole;
    private readonly IDeleteRoleUseCase _deleteRole;

    public RoleController(
        ICreateRoleUseCase createRole,
        IGetRoleUseCase getRole,
        IGetAllRolesUseCase getAllRoles,
        IUpdateRoleUseCase updateRole,
        IDeleteRoleUseCase deleteRole)
    {
        _createRole = createRole;
        _getRole = getRole;
        _getAllRoles = getAllRoles;
        _updateRole = updateRole;
        _deleteRole = deleteRole;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<RoleResponseDto>> Create([FromBody] CreateRoleRequestDto request)
    {
        var result = await _createRole.ExecuteAsync(request);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "admin,user")]
    public async Task<ActionResult<RoleResponseDto>> GetByCode(string code)
    {
        var result = await _getRole.ExecuteAsync(code);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin,user")]
    public async Task<ActionResult<List<RoleResponseDto>>> GetAll()
    {
        var result = await _getAllRoles.ExecuteAsync();
        return Ok(result);
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<RoleResponseDto>> Update(string code, [FromBody] UpdateRoleRequestDto request)
    {
        var result = await _updateRole.ExecuteAsync(code, request);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(string code)
    {
        var result = await _deleteRole.ExecuteAsync(code);
        if (!result)
            return NotFound();

        return NoContent();
    }
}