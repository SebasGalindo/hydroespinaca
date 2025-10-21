using BffService.Application.Interfaces;
using BffService.Domain.Constants;
using HydroEspinaca.Shared.DTOs.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with the Auth Service API
/// </summary>
public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthServiceClient> _logger;
    private readonly string _authServiceUrl;

    public AuthServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authServiceUrl = _configuration[BffConstants.Auth.AuthServiceUrlConfigKey]
            ?? throw new InvalidOperationException("AuthServiceUrl not configured");
    }

    #region User Operations

    public async Task<List<UserResponseDto>> GetAllUsersAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all users from auth service");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/users", cancellationToken);
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>(cancellationToken: cancellationToken);
            return users ?? new List<UserResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from auth service");
            throw;
        }
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(string id, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching user {UserId} from auth service", id);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/users/{id}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserResponseDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from auth service", id);
            throw;
        }
    }

    public async Task<UserResponseDto> CreateUserAsync(UserCreateDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating user {Email} in auth service", request.Email);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/api/users", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserResponseDto>(cancellationToken: cancellationToken);
            return user ?? throw new InvalidOperationException("Failed to create user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email} in auth service", request.Email);
            throw;
        }
    }

    public async Task<UserResponseDto> UpdateUserAsync(string id, UserUpdateDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating user {UserId} in auth service", id);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PutAsJsonAsync($"{_authServiceUrl}/api/users/{id}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserResponseDto>(cancellationToken: cancellationToken);
            return user ?? throw new InvalidOperationException("Failed to update user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId} in auth service", id);
            throw;
        }
    }

    public async Task DeleteUserAsync(string id, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting user {UserId} from auth service", id);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.DeleteAsync($"{_authServiceUrl}/api/users/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId} from auth service", id);
            throw;
        }
    }

    #endregion

    #region Role Operations

    public async Task<List<RoleResponseDto>> GetAllRolesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all roles from auth service");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/roles", cancellationToken);
            response.EnsureSuccessStatusCode();

            var roles = await response.Content.ReadFromJsonAsync<List<RoleResponseDto>>(cancellationToken: cancellationToken);
            return roles ?? new List<RoleResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching roles from auth service");
            throw;
        }
    }

    public async Task<RoleResponseDto?> GetRoleByCodeAsync(string code, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching role {RoleCode} from auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/roles/{code}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RoleResponseDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching role {RoleCode} from auth service", code);
            throw;
        }
    }

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating role {RoleCode} in auth service", request.Code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/api/roles", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var role = await response.Content.ReadFromJsonAsync<RoleResponseDto>(cancellationToken: cancellationToken);
            return role ?? throw new InvalidOperationException("Failed to create role");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleCode} in auth service", request.Code);
            throw;
        }
    }

    public async Task<RoleResponseDto> UpdateRoleAsync(string code, UpdateRoleRequestDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating role {RoleCode} in auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PutAsJsonAsync($"{_authServiceUrl}/api/roles/{code}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var role = await response.Content.ReadFromJsonAsync<RoleResponseDto>(cancellationToken: cancellationToken);
            return role ?? throw new InvalidOperationException("Failed to update role");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleCode} in auth service", code);
            throw;
        }
    }

    public async Task DeleteRoleAsync(string code, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting role {RoleCode} from auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.DeleteAsync($"{_authServiceUrl}/api/roles/{code}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleCode} from auth service", code);
            throw;
        }
    }

    #endregion

    #region Permission Operations

    public async Task<List<GroupedPermissionResponseDto>> GetGroupedPermissionsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching grouped permissions from auth service");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/permissions/grouped", cancellationToken);
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<GroupedPermissionResponseDto>>(cancellationToken: cancellationToken);
            return permissions ?? new List<GroupedPermissionResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching grouped permissions from auth service");
            throw;
        }
    }

    public async Task<List<PermissionResponseDto>> GetAllPermissionsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all permissions from auth service");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/permissions", cancellationToken);
            response.EnsureSuccessStatusCode();

            var permissions = await response.Content.ReadFromJsonAsync<List<PermissionResponseDto>>(cancellationToken: cancellationToken);
            return permissions ?? new List<PermissionResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permissions from auth service");
            throw;
        }
    }

    public async Task<PermissionResponseDto?> GetPermissionByCodeAsync(string code, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching permission {PermissionCode} from auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/permissions/{code}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PermissionResponseDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permission {PermissionCode} from auth service", code);
            throw;
        }
    }

    public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionRequestDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating permission {PermissionCode} in auth service", request.Code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/api/permissions", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var permission = await response.Content.ReadFromJsonAsync<PermissionResponseDto>(cancellationToken: cancellationToken);
            return permission ?? throw new InvalidOperationException("Failed to create permission");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {PermissionCode} in auth service", request.Code);
            throw;
        }
    }

    public async Task<PermissionResponseDto> UpdatePermissionAsync(string code, UpdatePermissionRequestDto request, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating permission {PermissionCode} in auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PutAsJsonAsync($"{_authServiceUrl}/api/permissions/{code}", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var permission = await response.Content.ReadFromJsonAsync<PermissionResponseDto>(cancellationToken: cancellationToken);
            return permission ?? throw new InvalidOperationException("Failed to update permission");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionCode} in auth service", code);
            throw;
        }
    }

    public async Task DeletePermissionAsync(string code, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting permission {PermissionCode} from auth service", code);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.DeleteAsync($"{_authServiceUrl}/api/permissions/{code}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionCode} from auth service", code);
            throw;
        }
    }

    #endregion

    #region Session Operations

    public async Task<List<UserSessionsDto>> GetAllSessionsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all sessions from auth service");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"{_authServiceUrl}/api/sessions", cancellationToken);
            response.EnsureSuccessStatusCode();

            var sessions = await response.Content.ReadFromJsonAsync<List<UserSessionsDto>>(cancellationToken: cancellationToken);
            return sessions ?? new List<UserSessionsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sessions from auth service");
            throw;
        }
    }

    public async Task RevokeSessionAsync(string sessionId, string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Revoking session {SessionId} in auth service", sessionId);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.DeleteAsync($"{_authServiceUrl}/api/sessions/{sessionId}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId} in auth service", sessionId);
            throw;
        }
    }

    #endregion
}
