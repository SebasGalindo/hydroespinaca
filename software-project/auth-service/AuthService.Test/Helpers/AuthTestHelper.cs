// No more Application layer dependencies
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AuthService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AuthService.Test.Helpers;

public class AuthTestHelper : IDisposable
{
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly IMongoDatabase _database;

    public AuthTestHelper(HttpClient client, IServiceProvider serviceProvider, IMongoDatabase database)
    {
        _client = client;
        _scope = serviceProvider.CreateScope();
        _database = database;
    }

    public async Task<string> GetAdminTokenAsync(List<string>? requiredPermissions = null)
    {
        try
        {
            // 1. Ensure data seeding is complete
            await EnsureDataSeedingCompletedAsync();
            
            // 2. Validate test users exist and have correct credentials
            var validationResult = await ValidateTestUsersBeforeTestAsync();
            if (!validationResult)
            {
                throw new Exception("Test user validation failed. Check logs for details.");
            }

            // 3. Verify admin has required permissions if specified
            if (requiredPermissions?.Any() == true)
            {
                await VerifyUserHasPermissionsAsync("admin@demo.com", requiredPermissions);
            }

            var loginRequest = TestDataHelper.CreateLoginRequest();
            Console.WriteLine($"[AuthTestHelper] Attempting admin login with email: admin@demo.com");

            var response = await _client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AuthTestHelper] Admin login failed with status: {response.StatusCode}");
                Console.WriteLine($"[AuthTestHelper] Response content: {content}");
                Console.WriteLine($"[AuthTestHelper] Request payload: {JsonSerializer.Serialize(loginRequest)}");
                
                // Enhanced diagnostics with new methods
                await LogUserDiagnosticsAsync("admin@demo.com");
                await LogUserExistenceAsync("admin@demo.com");
                await LogRolesAndPermissionsAsync();
                
                throw new Exception($"Admin login failed during test setup. Status: {response.StatusCode}, Content: {content}");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            if (!root.TryGetProperty("accessToken", out var accessTokenElement))
            {
                throw new Exception($"Token response is missing AccessToken. Content: {content}");
            }
            
            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception($"AccessToken is null or empty. Content: {content}");
            }

            Console.WriteLine($"[AuthTestHelper] Admin login successful, token length: {accessToken.Length}");
            
            // Validate token claims if required permissions were specified
            if (requiredPermissions?.Any() == true)
            {
                await ValidateTokenClaimsAsync(accessToken, requiredPermissions);
            }
            
            return accessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Exception in GetAdminTokenAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetUserTokenAsync(List<string>? requiredPermissions = null)
    {
        try
        {
            // 1. Ensure data seeding is complete
            await EnsureDataSeedingCompletedAsync();
            
            // 2. Validate test users exist and have correct credentials
            var validationResult = await ValidateTestUsersBeforeTestAsync();
            if (!validationResult)
            {
                throw new Exception("Test user validation failed. Check logs for details.");
            }

            // 3. Verify user has required permissions if specified
            if (requiredPermissions?.Any() == true)
            {
                await VerifyUserHasPermissionsAsync("user@demo.com", requiredPermissions);
            }

            var loginRequest = TestDataHelper.CreateLoginRequest("user@demo.com", "N16'+4a597|V!");
            Console.WriteLine($"[AuthTestHelper] Attempting user login with email: user@demo.com");

            var response = await _client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AuthTestHelper] User login failed with status: {response.StatusCode}");
                Console.WriteLine($"[AuthTestHelper] Response content: {content}");
                Console.WriteLine($"[AuthTestHelper] Request payload: {JsonSerializer.Serialize(loginRequest)}");
                
                // Enhanced diagnostics with new methods
                await LogUserDiagnosticsAsync("user@demo.com");
                await LogUserExistenceAsync("admin@demo.com");
                await LogRolesAndPermissionsAsync();
                
                throw new Exception($"User login failed during test setup. Status: {response.StatusCode}, Content: {content}");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            
            if (!root.TryGetProperty("accessToken", out var accessTokenElement))
            {
                throw new Exception($"User token response is missing AccessToken. Content: {content}");
            }
            
            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception($"User AccessToken is null or empty. Content: {content}");
            }

            Console.WriteLine($"[AuthTestHelper] User login successful, token length: {accessToken.Length}");
            
            // Validate token claims if required permissions were specified
            if (requiredPermissions?.Any() == true)
            {
                await ValidateTokenClaimsAsync(accessToken, requiredPermissions);
            }
            
            return accessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Exception in GetUserTokenAsync: {ex.Message}");
            throw;
        }
    }

    public void SetAuthorizationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthorizationHeader()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Creates authenticated HttpClient with token for isolated requests
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var handler = new HttpClientHandler();
        var client = new HttpClient(handler);
        client.BaseAddress = _client.BaseAddress;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task EnsureDataSeedingCompletedAsync()
    {
        try
        {
            Console.WriteLine("[AuthTestHelper] Starting data seeding...");
            
            // Use the centralized DataSeedingService instead of duplicating logic
            var seedingService = _scope.ServiceProvider.GetRequiredService<DataSeedingService>();
            
            Console.WriteLine("[AuthTestHelper] Calling SeedInitialDataAsync()...");
            await seedingService.SeedInitialDataAsync();
            Console.WriteLine("[AuthTestHelper] SeedInitialDataAsync() completed");
            
            // Quick verification of key entities
            var userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            
            var adminUser = await userRepository.FindByEmailAsync("admin@demo.com");
            var testUser = await userRepository.FindByEmailAsync("user@demo.com");
            var adminRole = await roleRepository.FindByCodeAsync("role_admin");
            var userRole = await roleRepository.FindByCodeAsync("role_user");
            
            Console.WriteLine($"[AuthTestHelper] Database verification after seeding:");
            Console.WriteLine($"  - Admin user exists: {adminUser != null}");
            Console.WriteLine($"  - Test user exists: {testUser != null}");
            Console.WriteLine($"  - Admin role exists: {adminRole != null}");
            Console.WriteLine($"  - User role exists: {userRole != null}");
            
            if (adminUser != null)
            {
                Console.WriteLine($"  - Admin user details: Email={adminUser.Email.Value}, RoleId={adminUser.RoleId}");
            }
            
            Console.WriteLine("[AuthTestHelper] Data seeding completed successfully");
            
            // Verify admin user was created
            await LogUserExistenceAsync("admin@demo.com");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] ERROR during data seeding: {ex.Message}");
            Console.WriteLine($"[AuthTestHelper] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task EnsureTestUserExistsAsync()
    {
        var userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Check if test user already exists
        var existingUser = await userRepository.FindByEmailAsync("user@demo.com");
        if (existingUser == null)
        {
            Console.WriteLine("[AuthTestHelper] Test user does not exist, creating...");
            // DataSeedingService should have created the role_user, just reference it
            var userRole = await roleRepository.FindByCodeAsync("role_user");
            if (userRole != null)
            {
                var hashedPassword = passwordHasher.Hash("N16'+4a597|V!");
                var testUser = new User(
                    new Email("user@demo.com"),
                    new HashedPassword(hashedPassword),
                    userRole.Id
                );
                await userRepository.CreateAsync(testUser);
                Console.WriteLine("[AuthTestHelper] Test user created successfully");
            }
            else
            {
                Console.WriteLine("[AuthTestHelper] ERROR: role_user not found, cannot create test user");
            }
        }
        else
        {
            Console.WriteLine("[AuthTestHelper] Test user already exists");
        }
    }

    private async Task LogUserExistenceAsync(string email)
    {
        try
        {
            var userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var user = await userRepository.FindByEmailAsync(email);
            
            if (user == null)
            {
                Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: User with email '{email}' does NOT exist in database");
            }
            else
            {
                Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: User with email '{email}' EXISTS in database");
                Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: User ID: {user.Id}, RoleId: {user.RoleId}");
                
                // Check if password hash looks correct
                if (user.Password?.Value?.StartsWith("$2a$") == true || user.Password?.Value?.StartsWith("$2b$") == true)
                {
                    Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Password hash looks valid (BCrypt format)");
                    Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Password hash: {user.Password.Value}");
                    
                    // Test password verification for admin user
                    if (email == "admin@demo.com")
                    {
                        var testPassword = "dF^J`c'662:W";
                        var verifyResult = passwordHasher.Verify(user.Password.Value, testPassword);
                        Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Password verification for admin user with '{testPassword}': {verifyResult}");
                        
                        // Try alternative verification approach
                        try
                        {
                            var alternativeResult = BCrypt.Net.BCrypt.Verify(testPassword, user.Password.Value);
                            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Alternative BCrypt verification: {alternativeResult}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Alternative BCrypt verification failed: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Password hash looks INVALID: {user.Password?.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Error checking user existence: {ex.Message}");
        }
    }

    private async Task LogRolesAndPermissionsAsync()
    {
        try
        {
            var roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var permissionRepository = _scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
            
            var adminRole = await roleRepository.FindByCodeAsync("role_admin");
            var userRole = await roleRepository.FindByCodeAsync("role_user");
            
            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Admin role exists: {adminRole != null}");
            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: User role exists: {userRole != null}");
            
            if (adminRole != null)
            {
                Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Admin role has {adminRole.Permissions.Count} permissions");
            }
            
            if (userRole != null)
            {
                Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: User role has {userRole.Permissions.Count} permissions");
            }
            
            // Count total permissions
            var allPermissions = await permissionRepository.GetAllAsync();
            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Total permissions in database: {allPermissions.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] DIAGNOSTIC: Error checking roles/permissions: {ex.Message}");
        }
    }

    private async Task VerifyUserHasPermissionsAsync(string email, List<string> requiredPermissions)
    {
        try
        {
            var userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var permissionRepository = _scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
            
            var user = await userRepository.FindByEmailAsync(email);
            if (user == null)
            {
                throw new Exception($"User {email} not found for permission verification");
            }
            
            var role = await roleRepository.FindByIdAsync(user.RoleId!);
            if (role == null)
            {
                throw new Exception($"Role not found for user {email}");
            }
            
            var userPermissions = await permissionRepository.FindByIdsAsync(role.Permissions);
            var userPermissionCodes = userPermissions.Select(p => p.Code).ToList();
            
            var missingPermissions = requiredPermissions.Except(userPermissionCodes).ToList();
            if (missingPermissions.Any())
            {
                Console.WriteLine($"[AuthTestHelper] WARNING: User {email} missing permissions: {string.Join(", ", missingPermissions)}");
                Console.WriteLine($"[AuthTestHelper] User has permissions: {string.Join(", ", userPermissionCodes)}");
            }
            else
            {
                Console.WriteLine($"[AuthTestHelper] User {email} has all required permissions: {string.Join(", ", requiredPermissions)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Error verifying user permissions: {ex.Message}");
        }
    }

    private async Task ValidateTokenClaimsAsync(string token, List<string> expectedPermissions)
    {
        try
        {
            Console.WriteLine($"[AuthTestHelper] Validating JWT token claims for permissions: {string.Join(", ", expectedPermissions)}");
            Console.WriteLine($"[AuthTestHelper] Token length: {token.Length}");
            
            // Decode JWT token without validation (for diagnostics)
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            Console.WriteLine($"[AuthTestHelper] Token issuer: {jsonToken.Issuer}");
            Console.WriteLine($"[AuthTestHelper] Token audience: {string.Join(", ", jsonToken.Audiences)}");
            Console.WriteLine($"[AuthTestHelper] Token expires: {jsonToken.ValidTo}");
            Console.WriteLine($"[AuthTestHelper] Token not before: {jsonToken.ValidFrom}");
            
            // Check basic claims
            var subClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
            var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "role");
            var jtiClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
            
            Console.WriteLine($"[AuthTestHelper] Token claims:");
            Console.WriteLine($"  - Subject (sub): {subClaim?.Value ?? "MISSING"}");
            Console.WriteLine($"  - Email: {emailClaim?.Value ?? "MISSING"}");
            Console.WriteLine($"  - Role: {roleClaim?.Value ?? "MISSING"}");
            Console.WriteLine($"  - JTI: {jtiClaim?.Value ?? "MISSING"}");
            
            // List all claims for debugging
            Console.WriteLine($"[AuthTestHelper] All token claims:");
            foreach (var claim in jsonToken.Claims)
            {
                Console.WriteLine($"  - {claim.Type}: {claim.Value}");
            }
            
            // Validate role is present
            if (roleClaim == null)
            {
                Console.WriteLine($"[AuthTestHelper] WARNING: Role claim missing from token");
            }
            else
            {
                Console.WriteLine($"[AuthTestHelper] Role claim found: {roleClaim.Value}");
                
                // Get user permissions based on role
                var roleRepository = _scope.ServiceProvider.GetRequiredService<IRoleRepository>();
                var permissionRepository = _scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
                
                var role = await roleRepository.FindByIdAsync(roleClaim.Value);
                if (role != null)
                {
                    var permissions = await permissionRepository.FindByIdsAsync(role.Permissions);
                    var permissionCodes = permissions.Select(p => p.Code).ToList();
                    
                    Console.WriteLine($"[AuthTestHelper] User role '{role.Code}' has permissions: [{string.Join(", ", permissionCodes)}]");
                    
                    // Check if user has all expected permissions
                    var missingPermissions = expectedPermissions.Except(permissionCodes).ToList();
                    if (missingPermissions.Any())
                    {
                        Console.WriteLine($"[AuthTestHelper] WARNING: User missing required permissions: {string.Join(", ", missingPermissions)}");
                    }
                    else
                    {
                        Console.WriteLine($"[AuthTestHelper] ✅ User has all required permissions");
                    }
                }
                else
                {
                    Console.WriteLine($"[AuthTestHelper] WARNING: Role '{roleClaim.Value}' not found in database");
                }
            }
            
            // Note: Actual token signature validation would require the public key
            // This is handled by the authentication middleware in the test host
            Console.WriteLine($"[AuthTestHelper] JWT token claims validation completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Error validating token claims: {ex.Message}");
            Console.WriteLine($"[AuthTestHelper] Stack trace: {ex.StackTrace}");
        }
    }

    public async Task<string> TestLoginIsolatedAsync(string email, string password)
    {
        Console.WriteLine($"[AuthTestHelper] Testing isolated login for {email}");
        
        try
        {
            var loginRequest = new { Email = email, Password = password };
            var response = await _client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[AuthTestHelper] Isolated login response: {response.StatusCode}");
            Console.WriteLine($"[AuthTestHelper] Isolated login content: {content}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;
                    if (root.TryGetProperty("accessToken", out var accessTokenElement))
                    {
                        return accessTokenElement.GetString() ?? "TOKEN_PARSE_ERROR";
                    }
                    return "TOKEN_PARSE_ERROR";
                }
                catch
                {
                    return "TOKEN_PARSE_ERROR";
                }
            }
            else
            {
                return $"LOGIN_FAILED_{response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Isolated login exception: {ex.Message}");
            return $"LOGIN_EXCEPTION_{ex.GetType().Name}";
        }
    }

    /// <summary>
    /// Validates that test users exist and have correct credentials before running tests
    /// </summary>
    private async Task<bool> ValidateTestUsersBeforeTestAsync()
    {
        try
        {
            var seedingService = _scope.ServiceProvider.GetRequiredService<DataSeedingService>();
            return await seedingService.ValidateTestUsersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Error validating test users: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets detailed diagnostics for a specific user
    /// </summary>
    private async Task LogUserDiagnosticsAsync(string email)
    {
        try
        {
            var seedingService = _scope.ServiceProvider.GetRequiredService<DataSeedingService>();
            var diagnostics = await seedingService.GetUserDiagnosticsAsync(email);
            Console.WriteLine($"[AuthTestHelper] User diagnostics: {diagnostics}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthTestHelper] Error getting user diagnostics: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}