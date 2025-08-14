using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface ICreatePermissionUseCase
{
    Task<PermissionResponseDto> ExecuteAsync(CreatePermissionRequestDto request);
}