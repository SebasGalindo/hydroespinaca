namespace AuthService.Application.DTOs;

public record PermissionResponseDto
{
    public string Id { get; init; } = null!; // ObjectId for internal use
    public string Code { get; init; } = null!; // Human-readable identifier
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}