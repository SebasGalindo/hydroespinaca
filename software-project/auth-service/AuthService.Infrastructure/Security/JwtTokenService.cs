using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
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

        public TokenResponseDto GenerateTokens(Guid userId, string email, string role, string? clientId)
        {
            var now = DateTime.UtcNow;

            // 1. Claims for access token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredNames.Email, email),
                new Claim("role", role),
                new Claim(JwtRegisteredNames.Jti, Guid.NewGuid().ToString())
            };
            if (!string.IsNullOrEmpty(clientId))
                claims.Add(new Claim("client_id", clientId));

            // 2. Obtain signing credentials
            var privateKeyPem = _keyStore.GetPrivateKey();
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());
            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256
            );

            // 3. Build token
            var jwtToken = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_settings.AccessTokenExpiryMinutes),
                signingCredentials: credentials
            );

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // 4. Generate random refresh token
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // 5. Return DTO
            return new TokenResponseDto
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshToken,
                ExpiresAt = jwtToken.ValidTo,
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
