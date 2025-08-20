using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using AuthService.Test.Models;
using MongoDB.Bson;

namespace AuthService.Test.Helpers;

public static class TestDataHelper
{
    public static Permission CreateTestPermission(string? code = null, string? name = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new Permission(
            code ?? $"perm_test_{uniqueId}",
            name ?? $"Test Permission {uniqueId}",
            $"Permission for testing purposes {uniqueId}"
        );
    }

    public static Role CreateTestRole(string? code = null, string? name = null, List<string>? permissionIds = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new Role(
            code ?? $"role_test_{uniqueId}",
            name ?? $"Test Role {uniqueId}", 
            permissionIds ?? new List<string>()
        );
    }

    public static User CreateTestUser(string? email = null, string? roleId = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new User(
            new Email(email ?? $"test{uniqueId}@example.com"),
            new HashedPassword("$2a$11$hashedpasswordhere"),
            roleId ?? ObjectId.GenerateNewId().ToString()
        );
    }

    public static ClientApp CreateTestClientApp(string? code = null, string? secret = null, IEnumerable<string>? scopes = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new ClientApp(
            code ?? $"client_test_{uniqueId}",
            new HashedPassword(secret ?? "$2a$11$hashedpasswordhere"),
            scopes ?? new[] { "read", "write" }
        );
    }

    // Simple request objects for HTTP testing - no DTOs from Application layer
    public static CreatePermissionRequestModel CreatePermissionRequest(string? code = null, string? name = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreatePermissionRequestModel
        {
            Code = code ?? $"perm_test_{uniqueId}",
            Name = name ?? $"Test Permission {uniqueId}",
            Description = $"Test description {uniqueId}"
        };
    }

    public static UpdatePermissionRequestModel UpdatePermissionRequest(string? name = null, string? description = null)
    {
        return new UpdatePermissionRequestModel
        {
            Name = name ?? "Updated Permission Name",
            Description = description ?? "Updated description"
        };
    }

    public static CreateRoleRequestModel CreateRoleRequest(string? code = null, string? name = null, List<string>? permissionCodes = null)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateRoleRequestModel
        {
            Code = code ?? $"role_test_{uniqueId}",
            Name = name ?? $"Test Role {uniqueId}",
            PermissionCodes = permissionCodes ?? new List<string>()
        };
    }

    public static UpdateRoleRequestModel UpdateRoleRequest(string? name = null, List<string>? permissionCodes = null)
    {
        return new UpdateRoleRequestModel
        {
            Name = name ?? "Updated Role Name",
            PermissionCodes = permissionCodes ?? new List<string>()
        };
    }

    public static LoginRequestModel CreateLoginRequest(string? email = null, string? password = null)
    {
        return new LoginRequestModel
        {
            Email = email ?? "admin@demo.com",
            Password = password ?? "Admin123!"
        };
    }

    public static object CreateClientCredentialsRequest(string? clientId = null, string? clientSecret = null)
    {
        return new 
        {
            ClientId = clientId ?? "test-client",
            ClientSecret = clientSecret ?? "test-secret"
        };
    }

    public static object CreateRefreshRequest(string? refreshToken = null, string? clientId = null)
    {
        return new 
        {
            RefreshToken = refreshToken ?? "test-refresh-token",
            ClientId = clientId ?? "test-client"
        };
    }
}