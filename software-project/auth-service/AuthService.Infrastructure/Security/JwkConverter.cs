using AuthService.Infrastructure.Security.Models;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Security;

public static class JwkConverter
{
    /// <summary>
    /// Converts an RSA public key PEM to a JSON Web Key (JWK) format.
    /// </summary>
    /// <param name="keyPair">The JWT key pair containing public key PEM and metadata</param>
    /// <returns>A JSON Web Key object</returns>
    public static JsonWebKey ToJsonWebKey(JwtKeyPair keyPair)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());
        
        var parameters = rsa.ExportParameters(false);
        
        return new JsonWebKey
        {
            KeyType = "RSA",
            KeyId = keyPair.KeyId,
            Use = "sig",
            Algorithm = "RS256",
            Modulus = Base64UrlEncode(parameters.Modulus!),
            Exponent = Base64UrlEncode(parameters.Exponent!)
        };
    }
    
    /// <summary>
    /// Creates a JSON Web Key Set (JWKS) from multiple key pairs.
    /// </summary>
    /// <param name="keyPairs">Collection of JWT key pairs</param>
    /// <returns>A JSON Web Key Set containing all public keys</returns>
    public static JsonWebKeySet ToJsonWebKeySet(IEnumerable<JwtKeyPair> keyPairs)
    {
        var jwks = new JsonWebKeySet();
        
        foreach (var keyPair in keyPairs)
        {
            jwks.Keys.Add(ToJsonWebKey(keyPair));
        }
        
        return jwks;
    }
    
    /// <summary>
    /// Encodes byte array to Base64Url format as required by JWK specification.
    /// </summary>
    /// <param name="bytes">Bytes to encode</param>
    /// <returns>Base64Url encoded string</returns>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}