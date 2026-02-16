using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Users.Queries.GetAllUsers;

/// <summary>
/// Query to retrieve all users in the system.
/// </summary>
public record GetAllUsersQuery() : IRequest<IEnumerable<UserResponseDto>>;