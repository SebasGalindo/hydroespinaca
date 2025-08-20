using AuthService.Domain.Enums;
using AuthService.Infrastructure.Security.Models;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Provides access to RSA key pairs for signing and validating JWTs.
    /// Supports separate key pairs for user tokens and machine-to-machine tokens.
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Gets the key pair for the specified token type.
        /// </summary>
        /// <param name="tokenType">The type of token (User or MachineToMachine)</param>
        /// <returns>The key pair containing private key, public key, and key ID</returns>
        JwtKeyPair GetKeyPair(TokenType tokenType);
        
        /// <summary>
        /// Gets all available key pairs.
        /// </summary>
        /// <returns>Collection of all key pairs</returns>
        IEnumerable<JwtKeyPair> GetAllKeyPairs();
        
        /// <summary>
        /// Gets a key pair by its key ID.
        /// </summary>
        /// <param name="keyId">The key identifier</param>
        /// <returns>The key pair if found, null otherwise</returns>
        JwtKeyPair? GetKeyPairById(string keyId);
    }
}
