using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IGetAllPermissionsUseCase
{
    Task<List<PermissionResponseDto>> ExecuteAsync();
}