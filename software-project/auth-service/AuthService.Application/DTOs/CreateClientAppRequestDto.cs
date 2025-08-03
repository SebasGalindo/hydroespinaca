namespace AuthService.Application.DTOs;
public record CreateClientAppRequestDto
{
    public string ClientId { get; init; } = default!;
    public string Secret { get; init; } = default!;
    public IEnumerable<string> Scopes { get; init; } = Array.Empty<string>();
}