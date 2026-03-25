namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for client credentials authentication request payload.
/// </summary>
public class ClientCredentialsRequestDto
{
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}