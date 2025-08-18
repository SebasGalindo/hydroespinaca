using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public class DataSeedingService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;


    public DataSeedingService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedTestUserAsync();
            await SeedTestUserAsync();
            await VerifyDataIntegrityAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Validates that test users exist and have correct credentials before running tests
    /// </summary>
    public async Task<bool> ValidateTestUsersAsync()
    {
        try
        {
            // Check admin user
            var adminUser = await _userRepository.FindByEmailAsync("admin@demo.com");
            if (adminUser == null)
            {
                return false;
            }

            var adminRole = await _roleRepository.FindByIdAsync(adminUser.RoleId!);
            if (adminRole == null)
            {
                return false;
            }

            // Verify admin password
            var adminPasswordValid = _passwordHasher.Verify(adminUser.Password.Value, "Admin123!");
            if (!adminPasswordValid)
            {
                return false;
            }

            // Check test user
            var testUser = await _userRepository.FindByEmailAsync("user@demo.com");
            if (testUser == null)
            {
                return false;
            }

            var userRole = await _roleRepository.FindByIdAsync(testUser.RoleId!);
            if (userRole == null)
            {
                return false;
            }

            // Verify test user password
            var userPasswordValid = _passwordHasher.Verify(testUser.Password.Value, "User123!");
            if (!userPasswordValid)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }

    }

    /// <summary>
    /// Gets detailed user information for diagnostics
    /// </summary>
    public async Task<string> GetUserDiagnosticsAsync(string email)
    {
        try
        {
            var user = await _userRepository.FindByEmailAsync(email);
            if (user == null)
            {
                return $"User {email} not found";
            }

            var role = await _roleRepository.FindByIdAsync(user.RoleId!);
            if (role == null)
            {
                return $"User {email} exists but role not found";
            }

            var permissions = await _permissionRepository.FindByIdsAsync(role.Permissions);
            var permissionCodes = permissions.Select(p => p.Code).ToList();

            return $"User: {email}, Role: {role.Code}, Permissions: [{string.Join(", ", permissionCodes)}]";
        }
        catch (Exception ex)
        {
            return $"Error getting diagnostics for {email}: {ex.Message}";
        }
    }

    private async Task SeedPermissionsAsync()
    {

        var permissions = new[]
        {
            new Permission("perm_user_create", "user:create", "Create new users"),
            new Permission("perm_user_read", "user:read", "Read user information"),
            new Permission("perm_user_update", "user:update", "Update user information"),
            new Permission("perm_user_delete", "user:delete", "Delete users"),
            new Permission("perm_role_create", "role:create", "Create new roles"),
            new Permission("perm_role_read", "role:read", "Read role information"),
            new Permission("perm_role_update", "role:update", "Update role information"),
            new Permission("perm_role_delete", "role:delete", "Delete roles"),
            new Permission("perm_permission_create", "permission:create", "Create new permissions"),
            new Permission("perm_permission_read", "permission:read", "Read permission information"),
            new Permission("perm_permission_update", "permission:update", "Update permission information"),
            new Permission("perm_permission_delete", "permission:delete", "Delete permissions")
        };

        var createdCount = 0;
        foreach (var permission in permissions)
        {
            var existing = await _permissionRepository.FindByCodeAsync(permission.Code);
            if (existing == null)
            {
                await _permissionRepository.CreateAsync(permission);
                createdCount++;
            }
        }
    }

    private async Task SeedRolesAsync()
    {
        var adminPermissionCodes = new[]
        {
            "perm_user_create", "perm_user_read", "perm_user_update", "perm_user_delete",
            "perm_role_create", "perm_role_read", "perm_role_update", "perm_role_delete",
            "perm_permission_create", "perm_permission_read", "perm_permission_update", "perm_permission_delete"
        };

        var adminPermissions = await _permissionRepository.FindByCodesAsync(adminPermissionCodes);
        var adminPermissionIds = adminPermissions.Select(p => p.Id).ToList();

        var adminRole = new Role("role_admin", "Administrator", adminPermissionIds);
        var existingAdminRole = await _roleRepository.FindByCodeAsync(adminRole.Code);
        if (existingAdminRole == null)
        {
            await _roleRepository.CreateAsync(adminRole);
        }

        // User role with basic permissions
        var userPermissionCodes = new[]
        {
            "perm_user_read", "perm_permission_read", "perm_role_read"
        };

        var userPermissions = await _permissionRepository.FindByCodesAsync(userPermissionCodes);
        var userPermissionIds = userPermissions.Select(p => p.Id).ToList();

        var userRole = new Domain.Entities.Role("role_user", "User", userPermissionIds);
        var existingUserRole = await _roleRepository.FindByCodeAsync(userRole.Code);
        if (existingUserRole == null)
        {
            await _roleRepository.CreateAsync(userRole);
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@demo.com";
        const string adminPassword = "Admin123!";


        var existingUser = await _userRepository.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            var adminRole = await _roleRepository.FindByCodeAsync("role_admin");
            if (adminRole == null)
            {
                return;
            }

            var hashedPassword = _passwordHasher.Hash(adminPassword);

            var immediateVerification = _passwordHasher.Verify(hashedPassword, adminPassword);

            if (!immediateVerification)
            {
                var alternativeHash = _passwordHasher.Hash(adminPassword);
                var alternativeVerification = _passwordHasher.Verify(alternativeHash, adminPassword);
            }

            var adminUser = new User(
                new Email(adminEmail),
                new HashedPassword(hashedPassword),
                adminRole.Id
            );

            await _userRepository.CreateAsync(adminUser);

            var postSaveVerification = _passwordHasher.Verify(hashedPassword, adminPassword);
        }
        else
        {
            var verificationResult = _passwordHasher.Verify(existingUser.Password.Value, adminPassword);

            if (!verificationResult)
            {
                var newHash = _passwordHasher.Hash(adminPassword);
                var newHashVerification = _passwordHasher.Verify(newHash, adminPassword);
            }
        }
    }

    private async Task SeedTestUserAsync()
    {
        const string userEmail = "user@demo.com";
        const string userPassword = "User123!";


        var existingUser = await _userRepository.FindByEmailAsync(userEmail);
        if (existingUser == null)
        {
            // Resolve role code to ObjectId
            var userRole = await _roleRepository.FindByCodeAsync("role_user");
            if (userRole == null)
            {
                return;
            }

            var hashedPassword = _passwordHasher.Hash(userPassword);

            var testUser = new User(
                new Email(userEmail),
                new HashedPassword(hashedPassword),
                userRole.Id
            );

            await _userRepository.CreateAsync(testUser);

            var verificationResult = _passwordHasher.Verify(hashedPassword, userPassword);
        }
        else
        {
            // Test the existing hash
            var verificationResult = _passwordHasher.Verify(existingUser.Password.Value, userPassword);
        }
    }

    private async Task VerifyDataIntegrityAsync()
    {
        // Verify permissions
        var allPermissions = await _permissionRepository.GetAllAsync();

        // Verify roles
        var adminRole = await _roleRepository.FindByCodeAsync("role_admin");
        var userRole = await _roleRepository.FindByCodeAsync("role_user");

        // Verify users
        var adminUser = await _userRepository.FindByEmailAsync("admin@demo.com");
        var testUser = await _userRepository.FindByEmailAsync("user@demo.com");
    }
}