using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    string Id,
    string? Username = null,
    string? Email = null,
    string? Password = null,
    string? RoleId = null
) : IRequest<UserResponseDto>;