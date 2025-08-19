using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using JwtRegisteredNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Service to generate and validate JWT using RS256 with support for multiple key pairs.
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private readonly IKeyStore _keyStore;
        private readonly JwtSettings _settings;

        public JwtTokenService(IKeyStore keyStore, IOptions<JwtSettings> options)
        {
            _keyStore = keyStore;
            _settings = options.Value;
        }

        public TokenResult GenerateTokens(string userId, string email, string role, string? clientId, TokenType tokenType = TokenType.User)
        {
            var now = DateTime.UtcNow;
            
            // Ensure minimum expiry time to avoid NotBefore/Expires collision
            var expiryMinutes = Math.Max(_settings.AccessTokenExpiryMinutes, 1);

            // Get the appropriate key pair for the token type
            var keyPair = _keyStore.GetKeyPair(tokenType);

            // 1. Claims for access token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredNames.Sub, userId),
                new Claim(JwtRegisteredNames.Email, email),
                new Claim("role", role), // Use simple "role" claim for better microservices compatibility
                new Claim(JwtRegisteredNames.Jti, Guid.NewGuid().ToString())
            };
            if (!string.IsNullOrEmpty(clientId))
                claims.Add(new Claim("client_id", clientId));

            // 2. Build token - ensure notBefore is slightly before expires
            var notBefore = now.AddSeconds(-1); // 1 second before now
            var expires = now.AddMinutes(expiryMinutes);

            // 3. Create RSA instance and keep it alive - don't dispose manually
            var rsa = RSA.Create();
            
            // Import the private key for the specified token type
            rsa.ImportFromPem(keyPair.PrivateKey.ToCharArray());
            
            // Create signing credentials with KID
            var rsaKey = new RsaSecurityKey(rsa) { KeyId = keyPair.KeyId };
            var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
            
            var jwtToken = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: notBefore,
                expires: expires,
                signingCredentials: credentials
            );

            // Generate the token string
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // 4. Generate random refresh token
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // 5. Return TokenResult (let GC handle RSA disposal)
            return new TokenResult
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshToken,
                ExpiresAt = expires,
                Role = role,
                ClientId = clientId
            };
        }

        public bool IsTokenValid(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                
                // Extract the KID from the token header
                var kid = jsonToken.Header.Kid;
                if (string.IsNullOrEmpty(kid))
                {
                    // Fallback to user token type for legacy tokens without KID
                    kid = _keyStore.GetKeyPair(TokenType.User).KeyId;
                }

                // Get the key pair by KID
                var keyPair = _keyStore.GetKeyPairById(kid);
                if (keyPair == null)
                {
                    return false;
                }

                using var rsa = RSA.Create();
                rsa.ImportFromPem(keyPair.PublicKey.ToCharArray());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = kid },
                };

                handler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Legacy method for backward compatibility
        public TokenResult GenerateTokens(string userId, string email, string role, string? clientId)
        {
            return GenerateTokens(userId, email, role, clientId, TokenType.User);
        }
    }
}
