using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Domain.Settings;
using AuthService.Infrastructure.Security.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using JwtRegisteredNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Service to generate and validate JWT using RS256 with support for multiple key pairs.
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private readonly IKeyStore _keyStore;
        private readonly JwtSettings _settings;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public JwtTokenService(
            IKeyStore keyStore, 
            IOptions<JwtSettings> options,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository)
        {
            _keyStore = keyStore;
            _settings = options.Value;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
        }

        public async Task<TokenResult> GenerateTokensAsync(string userId, string email, string role, string? clientId, TokenType tokenType = TokenType.User)
        {
            return await GenerateTokensAsync(userId, email, role, clientId, tokenType, null);
        }
        
        public async Task<TokenResult> GenerateTokensAsync(string userId, string email, string role, string? clientId, TokenType tokenType, string[]? explicitScopes)
        {
            var now = DateTime.UtcNow;
            
            // Ensure minimum expiry time to avoid NotBefore/Expires collision
            var expiryMinutes = Math.Max(_settings.AccessTokenExpiryMinutes, 1);

            // Get the appropriate key pair for the token type
            var keyPair = _keyStore.GetKeyPair(tokenType);

            // 1. Get scopes directly from permissions (permission.code is the scope)
            string[] scopes;
            if (tokenType == TokenType.MachineToMachine && !string.IsNullOrEmpty(clientId))
            {
                // For M2M tokens, use explicit scopes from database if provided
                if (explicitScopes != null && explicitScopes.Length > 0)
                {
                    scopes = explicitScopes;
                }
                else
                {
                    // M2M tokens require explicit scopes - empty array if none provided
                    scopes = Array.Empty<string>();
                }
            }
            else
            {
                // For user tokens, get permissions from role - permission.code is the scope
                var permissions = await GetUserPermissionsAsync(role);
                scopes = permissions.ToArray();
                
                // Add profile scopes for all authenticated users
                var allScopes = new List<string>(scopes);
                allScopes.Add(HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead);
                allScopes.Add(HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileUpdate);
                
                // Add system admin scope for admin users
                if (role == HydroEspinaca.Shared.Constants.SystemRoles.Admin)
                {
                    allScopes.Add(HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin);
                }
                
                scopes = allScopes.Distinct().ToArray();
            }

            // 2. Claims for access token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredNames.Sub, userId),
                new Claim(JwtRegisteredNames.Email, email),
                new Claim("role", role), // Use simple "role" claim for better microservices compatibility
                new Claim(JwtRegisteredNames.Jti, Guid.NewGuid().ToString())
            };
            
            if (!string.IsNullOrEmpty(clientId))
                claims.Add(new Claim("client_id", clientId));
                
            // Add scope claims - use space-separated string as per OAuth 2.0 spec
            if (scopes.Length > 0)
            {
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            }

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
                ClientId = clientId,
                Scopes = scopes
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

        /// <summary>
        /// Gets user permissions based on their role
        /// </summary>
        private async Task<IEnumerable<string>> GetUserPermissionsAsync(string roleCode)
        {
            try
            {
                // Find role by code
                var role = await _roleRepository.FindByCodeAsync(roleCode);
                if (role == null)
                {
                    return Array.Empty<string>();
                }

                // Get permissions for the role - permission.code is already the scope
                // Role.Permissions is a list of permission codes (scopes)
                return role.Permissions;
            }
            catch
            {
                // Return empty permissions on error to avoid token generation failure
                return Array.Empty<string>();
            }
        }
    }
}
