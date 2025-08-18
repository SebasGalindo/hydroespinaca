// Simple models for tests that don't depend on Application layer
namespace AuthService.Test.Models;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? ClientId { get; set; }
}

public class ClientCredentialsRequestDto
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Role { get; set; }
    public string? ClientId { get; set; }
}

public class PermissionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RoleResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> PermissionCodes { get; set; } = new();
}

// Request models for test helpers
public class CreatePermissionRequestModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdatePermissionRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateRoleRequestModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> PermissionCodes { get; set; } = new();
}

public class UpdateRoleRequestModel
{
    public string Name { get; set; } = string.Empty;
    public List<string> PermissionCodes { get; set; } = new();
}

public class LoginRequestModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}