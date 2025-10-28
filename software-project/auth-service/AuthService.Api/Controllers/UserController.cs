using AuthService.Application.Features.Users.Commands.CreateUser;
using AuthService.Application.Features.Users.Commands.DeleteUser;
using AuthService.Application.Features.Users.Commands.UpdateUser;
using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Application.Features.Users.Queries.GetAllUsers;
using AuthService.Application.Features.Users.Queries.GetUser;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.UserCreate)]
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] UserCreateDto request)
    {
        var command = new CreateUserCommand(request.Username, request.Email, request.Password, request.RoleId);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.UserRead)]
    public async Task<ActionResult<UserResponseDto>> GetById(string id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.UserRead)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
    {
        var query = new GetAllUsersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.UserUpdate)]
    public async Task<ActionResult<UserResponseDto>> Update(string id, [FromBody] UserUpdateDto request)
    {
        var command = new UpdateUserCommand(id, request.Username, request.Email, request.Password, request.RoleId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.UserDelete)]
    public async Task<ActionResult> Delete(string id)
    {
        // Get current user ID from JWT claims
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Prevent self-deletion
        if (currentUserId == id)
        {
            return BadRequest(new { message = "No puedes eliminar tu propia cuenta de usuario" });
        }

        var command = new DeleteUserCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}