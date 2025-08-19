using AuthService.Domain.Enums;
using AuthService.Infrastructure.Security.Models;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Security;

public static class RsaKeyGenerator
{
    /// <summary>
    /// Generates a new RSA key pair with the specified key ID and token type.
    /// </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="tokenType">The token type this key pair will be used for</param>
    /// <param name="keySize">RSA key size in bits (default: 2048)</param>
    /// <returns>A new JWT key pair</returns>
    public static JwtKeyPair GenerateKeyPair(string keyId, TokenType tokenType, int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);
        
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        
        return new JwtKeyPair
        {
            KeyId = keyId,
            PrivateKey = privateKeyPem,
            PublicKey = publicKeyPem,
            TokenType = tokenType
        };
    }
    
    /// <summary>
    /// Generates default key IDs for user and M2M token types.
    /// </summary>
    /// <param name="tokenType">The token type</param>
    /// <returns>A default key ID</returns>
    public static string GenerateDefaultKeyId(TokenType tokenType)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return tokenType switch
        {
            TokenType.User => $"user-{timestamp}",
            TokenType.MachineToMachine => $"m2m-{timestamp}",
            _ => throw new ArgumentException($"Unknown token type: {tokenType}")
        };
    }
}