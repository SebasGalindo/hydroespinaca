using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IGetAllRolesUseCase
{
    Task<List<RoleResponseDto>> ExecuteAsync();
}