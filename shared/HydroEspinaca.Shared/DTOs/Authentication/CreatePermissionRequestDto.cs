namespace HydroEspinaca.Shared.DTOs.Authentication;

public record CreatePermissionRequestDto
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
