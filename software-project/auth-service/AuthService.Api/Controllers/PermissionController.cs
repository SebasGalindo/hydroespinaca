using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionController : ControllerBase
{
    private readonly ICreatePermissionUseCase _createPermission;
    private readonly IGetPermissionUseCase _getPermission;
    private readonly IGetAllPermissionsUseCase _getAllPermissions;
    private readonly IUpdatePermissionUseCase _updatePermission;
    private readonly IDeletePermissionUseCase _deletePermission;

    public PermissionController(
        ICreatePermissionUseCase createPermission,
        IGetPermissionUseCase getPermission,
        IGetAllPermissionsUseCase getAllPermissions,
        IUpdatePermissionUseCase updatePermission,
        IDeletePermissionUseCase deletePermission)
    {
        _createPermission = createPermission;
        _getPermission = getPermission;
        _getAllPermissions = getAllPermissions;
        _updatePermission = updatePermission;
        _deletePermission = deletePermission;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PermissionResponseDto>> Create([FromBody] CreatePermissionRequestDto request)
    {
        var result = await _createPermission.ExecuteAsync(request);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = "admin,user")]
    public async Task<ActionResult<PermissionResponseDto>> GetByCode(string code)
    {
        var result = await _getPermission.ExecuteAsync(code);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin,user")]
    public async Task<ActionResult<List<PermissionResponseDto>>> GetAll()
    {
        var result = await _getAllPermissions.ExecuteAsync();
        return Ok(result);
    }

    [HttpPut("{code}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PermissionResponseDto>> Update(string code, [FromBody] UpdatePermissionRequestDto request)
    {
        var result = await _updatePermission.ExecuteAsync(code, request);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{code}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(string code)
    {
        var result = await _deletePermission.ExecuteAsync(code);
        if (!result)
            return NotFound();

        return NoContent();
    }
}