using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IGetPermissionUseCase
{
    Task<PermissionResponseDto?> ExecuteAsync(string id);
}