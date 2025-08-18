using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using JwtRegisteredNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Service to generate and validate JWT using RS256.
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

        public TokenResult GenerateTokens(string userId, string email, string role, string? clientId)
        {
            var now = DateTime.UtcNow;
            
            // Ensure minimum expiry time to avoid NotBefore/Expires collision
            var expiryMinutes = Math.Max(_settings.AccessTokenExpiryMinutes, 1);

            // 1. Claims for access token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredNames.Sub, userId),
                new Claim(JwtRegisteredNames.Email, email),
                new Claim(ClaimTypes.Role, role), // Use ClaimTypes.Role for ASP.NET Core authorization
                new Claim(JwtRegisteredNames.Jti, Guid.NewGuid().ToString())
            };
            if (!string.IsNullOrEmpty(clientId))
                claims.Add(new Claim("client_id", clientId));

            // 2. Build token - ensure notBefore is slightly before expires
            var notBefore = now.AddSeconds(-1); // 1 second before now
            var expires = now.AddMinutes(expiryMinutes);

            // 3. Create RSA instance and keep it alive - don't dispose manually
            var rsa = RSA.Create();
            
            // Import the private key
            var privateKeyPem = _keyStore.GetPrivateKey();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());
            
            // Create signing credentials
            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256
            );
            
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
                var publicKeyPem = _keyStore.GetPublicKey();
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem.ToCharArray());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                };


                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
