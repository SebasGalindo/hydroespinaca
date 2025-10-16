using AuthService.Application.Features.Users.DTOs;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string? RoleId = null
) : IRequest<UserResponseDto>;