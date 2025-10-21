using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for interacting with the Auth Service API for user, role, and permission management
/// </summary>
public interface IAuthServiceClient
{
    // User operations
    Task<List<UserResponseDto>> GetAllUsersAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetUserByIdAsync(string id, string accessToken, CancellationToken cancellationToken = default);
    Task<UserResponseDto> CreateUserAsync(UserCreateDto request, string accessToken, CancellationToken cancellationToken = default);
    Task<UserResponseDto> UpdateUserAsync(string id, UserUpdateDto request, string accessToken, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string id, string accessToken, CancellationToken cancellationToken = default);

    // Role operations
    Task<List<RoleResponseDto>> GetAllRolesAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<RoleResponseDto?> GetRoleByCodeAsync(string code, string accessToken, CancellationToken cancellationToken = default);
    Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto request, string accessToken, CancellationToken cancellationToken = default);
    Task<RoleResponseDto> UpdateRoleAsync(string code, UpdateRoleRequestDto request, string accessToken, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(string code, string accessToken, CancellationToken cancellationToken = default);

    // Permission operations
    Task<List<GroupedPermissionResponseDto>> GetGroupedPermissionsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<List<PermissionResponseDto>> GetAllPermissionsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<PermissionResponseDto?> GetPermissionByCodeAsync(string code, string accessToken, CancellationToken cancellationToken = default);
    Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto request, string accessToken, CancellationToken cancellationToken = default);
    Task<PermissionResponseDto> UpdatePermissionAsync(string code, UpdatePermissionRequestDto request, string accessToken, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(string code, string accessToken, CancellationToken cancellationToken = default);

    // Session operations
    Task<List<UserSessionsDto>> GetAllSessionsAsync(string accessToken, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string sessionId, string accessToken, CancellationToken cancellationToken = default);
}
