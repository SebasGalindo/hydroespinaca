using AuthService.Application.Features.Users.Commands.CreateUser;
using AuthService.Application.Features.Users.Commands.DeleteUser;
using AuthService.Application.Features.Users.Commands.UpdateUser;
using AuthService.Application.Features.Users.DTOs;
using AuthService.Application.Features.Users.Queries.GetAllUsers;
using AuthService.Application.Features.Users.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] UserCreateDto request)
    {
        var command = new CreateUserCommand(request.Email, request.Password, request.RoleId);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "role_admin,role_user")]
    public async Task<ActionResult<UserResponseDto>> GetById(string id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
    {
        var query = new GetAllUsersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult<UserResponseDto>> Update(string id, [FromBody] UserUpdateDto request)
    {
        var command = new UpdateUserCommand(id, request.Email, request.Password, request.RoleId);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "role_admin")]
    public async Task<ActionResult> Delete(string id)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}