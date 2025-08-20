using AuthService.Domain.Enums;
using AuthService.Infrastructure.Security.Models;

namespace AuthService.Infrastructure.Security;

/// <summary>
/// Implementación en memoria de IKeyStore para usar en tests.
/// Genera claves RSA temporales que son válidas para la duración de la prueba.
/// </summary>
public class InMemoryTestKeyStore : IKeyStore
{
    private readonly Dictionary<TokenType, JwtKeyPair> _keyPairs;
    private readonly Dictionary<string, JwtKeyPair> _keyPairsById;

    public InMemoryTestKeyStore()
    {
        _keyPairs = new Dictionary<TokenType, JwtKeyPair>();
        _keyPairsById = new Dictionary<string, JwtKeyPair>();
        
        // Generate key pairs for each token type
        foreach (TokenType tokenType in Enum.GetValues<TokenType>())
        {
            var keyId = $"test-{tokenType.ToString().ToLower()}-{Guid.NewGuid():N}";
            var keyPair = RsaKeyGenerator.GenerateKeyPair(keyId, tokenType);
            
            _keyPairs[tokenType] = keyPair;
            _keyPairsById[keyId] = keyPair;
        }
    }

    public JwtKeyPair GetKeyPair(TokenType tokenType)
    {
        if (_keyPairs.TryGetValue(tokenType, out var keyPair))
        {
            return keyPair;
        }
        
        throw new InvalidOperationException($"No key pair found for token type: {tokenType}");
    }

    public IEnumerable<JwtKeyPair> GetAllKeyPairs()
    {
        return _keyPairs.Values;
    }

    public JwtKeyPair? GetKeyPairById(string keyId)
    {
        _keyPairsById.TryGetValue(keyId, out var keyPair);
        return keyPair;
    }

    // Legacy methods for backward compatibility
    public string GetPrivateKey()
    {
        return GetKeyPair(TokenType.User).PrivateKey;
    }

    public string GetPublicKey()
    {
        return GetKeyPair(TokenType.User).PublicKey;
    }
}