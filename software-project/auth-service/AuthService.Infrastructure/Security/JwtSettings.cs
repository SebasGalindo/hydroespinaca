namespace AuthService.Infrastructure.Security;
/// <summary>
/// JWT configuration: issuer, audience, expirations.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenExpiryMinutes { get; set; }
    public int RefreshTokenExpiryDays { get; set; }
}