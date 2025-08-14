namespace AuthService.Application.DTOs;

public record RoleResponseDto
{
    public string Id { get; init; } = null!; // ObjectId for internal use
    public string Code { get; init; } = null!; // Human-readable identifier
    public string Name { get; init; } = null!;
    public List<string> PermissionCodes { get; init; } = new(); // Permission codes for API response
}