using AuthService.Application.Features.Users.DTOs;
using MediatR;

namespace AuthService.Application.Features.Users.Queries.GetUser;

public record GetUserQuery(string Id) : IRequest<UserResponseDto?>;