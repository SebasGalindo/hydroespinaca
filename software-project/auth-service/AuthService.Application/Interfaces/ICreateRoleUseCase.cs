using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface ICreateRoleUseCase
{
    Task<RoleResponseDto> ExecuteAsync(CreateRoleRequestDto request);
}