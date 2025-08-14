using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IUpdateRoleUseCase
{
    Task<RoleResponseDto?> ExecuteAsync(string id, UpdateRoleRequestDto request);
}