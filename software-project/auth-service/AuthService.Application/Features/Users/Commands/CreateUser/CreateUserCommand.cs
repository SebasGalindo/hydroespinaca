using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user with email, password, and role assignment.
/// </summary>
public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string? RoleId = null
) : IRequest<UserResponseDto>;