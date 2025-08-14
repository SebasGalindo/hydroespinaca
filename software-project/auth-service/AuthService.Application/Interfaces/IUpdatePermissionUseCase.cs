using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IUpdatePermissionUseCase
{
    Task<PermissionResponseDto?> ExecuteAsync(string id, UpdatePermissionRequestDto request);
}