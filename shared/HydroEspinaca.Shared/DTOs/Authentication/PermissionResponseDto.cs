using System.Text.Json.Serialization;

namespace HydroEspinaca.Shared.DTOs.Authentication;

public record PermissionResponseDto
{
    public string Id { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}
