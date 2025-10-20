namespace HydroEspinaca.Shared.DTOs.Authentication;

public record UpdatePermissionRequestDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}
