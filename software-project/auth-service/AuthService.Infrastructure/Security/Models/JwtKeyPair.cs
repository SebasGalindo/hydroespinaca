using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Security.Models;

public class JwtKeyPair
{
    public string KeyId { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
    public TokenType TokenType { get; set; }
}