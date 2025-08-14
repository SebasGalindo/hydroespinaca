using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IGetRoleUseCase
{
    Task<RoleResponseDto?> ExecuteAsync(string id);
}