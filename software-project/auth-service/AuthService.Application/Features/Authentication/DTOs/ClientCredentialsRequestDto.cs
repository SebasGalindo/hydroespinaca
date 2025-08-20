namespace AuthService.Application.Features.Authentication.DTOs;
public class ClientCredentialsRequestDto
{
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}