using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Users.Queries.GetUser;

/// <summary>
/// Query to retrieve a specific user by their identifier.
/// </summary>
public record GetUserQuery(string Id) : IRequest<UserResponseDto?>;