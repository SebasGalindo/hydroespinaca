namespace HydroEspinaca.Shared.DTOs.Authentication;
public record GroupedPermissionResponseDto
{
    public string Category { get; init; } = null!;
    public IReadOnlyCollection<PermissionResponseDto> Permissions { get; init; } = [];
}