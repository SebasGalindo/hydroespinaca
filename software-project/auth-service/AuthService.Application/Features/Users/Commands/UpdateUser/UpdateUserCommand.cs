using AuthService.Application.Features.Users.DTOs;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    string Id,
    string? Email = null,
    string? Password = null,
    string? RoleId = null
) : IRequest<UserResponseDto>;