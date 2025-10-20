using HydroEspinaca.Shared.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Features.Users.Queries.GetUser;

public record GetUserQuery(string Id) : IRequest<UserResponseDto?>;