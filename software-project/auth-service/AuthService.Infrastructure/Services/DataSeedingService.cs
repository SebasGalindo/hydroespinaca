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
            // User management permissions - ID descriptivo, Code es el scope exacto
            new Permission("user-create-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.UserCreate, "Create new users"),
            new Permission("user-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, "Read user information"),
            new Permission("user-update-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.UserUpdate, "Update user information"),
            new Permission("user-delete-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.UserDelete, "Delete users"),
            
            // Profile management permissions (self-service)
            new Permission("profile-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead, "Read own profile information"),
            new Permission("profile-update-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileUpdate, "Update own profile information"),
            
            // Role management permissions
            new Permission("role-create-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleCreate, "Create new roles"),
            new Permission("role-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, "Read role information"),
            new Permission("role-update-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleUpdate, "Update role information"),
            new Permission("role-delete-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleDelete, "Delete roles"),
            
            // Permission management permissions
            new Permission("permission-create-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionCreate, "Create new permissions"),
            new Permission("permission-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead, "Read permission information"),
            new Permission("permission-update-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionUpdate, "Update permission information"),
            new Permission("permission-delete-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionDelete, "Delete permissions"),
            
            // IoT Hardware permissions
            new Permission("sensor-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead, "Read sensor data and information"),
            new Permission("sensor-write-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorWrite, "Write sensor data and configuration"),
            new Permission("actuator-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorRead, "Read actuator status and information"),
            new Permission("actuator-control-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorControl, "Control actuator operations"),
            
            // ESP32 Node permissions
            new Permission("esp32-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Read, "Read ESP32 node information"),
            new Permission("esp32-write-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Write, "Write ESP32 node configuration"),
            new Permission("esp32-control-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Control, "Control ESP32 node operations"),
            
            // Variable management permissions
            new Permission("variable-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead, "Read variable information"),
            new Permission("variable-write-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableWrite, "Write variable data and configuration"),
            
            // Alert management permissions
            new Permission("alert-read-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertRead, "Read alert information"),
            new Permission("alert-write-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertWrite, "Write alert data and configuration"),
            new Permission("alert-manage-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertManage, "Manage alert rules and configuration"),
            
            // System-level permissions
            new Permission("system-admin-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin, "System administration access"),
            new Permission("system-health-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth, "Access system health information"),
            new Permission("system-monitor-permission", HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor, "Monitor system operations")
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
        // Admin role with full management permissions - usar los CODES (scopes), no los IDs
        var adminPermissionCodes = new[]
        {
            // User management
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserCreate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserUpdate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserDelete,
            // Role management
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleCreate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleUpdate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleDelete,
            // Permission management
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionCreate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionUpdate, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionDelete,
            // IoT Hardware (for admin dashboard)
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Read,
            // Data and monitoring
            HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableWrite, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertWrite, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertManage,
            // System access
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
        };

        var adminPermissions = await _permissionRepository.FindByCodesAsync(adminPermissionCodes);
        var adminPermissionIds = adminPermissions.Select(p => p.Id).ToList();

        var adminRole = new Role(HydroEspinaca.Shared.Constants.SystemRoles.Admin, "Administrator", adminPermissionIds);
        var existingAdminRole = await _roleRepository.FindByCodeAsync(adminRole.Code);
        if (existingAdminRole == null)
        {
            await _roleRepository.CreateAsync(adminRole);
        }

        // Regular user role with basic read permissions
        var userPermissionCodes = new[]
        {
            // Basic read permissions
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead,
            // Profile management (added automatically by token service)
            // System health for basic users
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
        };

        var userPermissions = await _permissionRepository.FindByCodesAsync(userPermissionCodes);
        var userPermissionIds = userPermissions.Select(p => p.Id).ToList();

        var userRole = new Domain.Entities.Role(HydroEspinaca.Shared.Constants.SystemRoles.User, "User", userPermissionIds);
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
            var adminRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.Admin);
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
            var userRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.User);
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
        var adminRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.Admin);
        var userRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.User);

        // Verify users
        var adminUser = await _userRepository.FindByEmailAsync("admin@demo.com");
        var testUser = await _userRepository.FindByEmailAsync("user@demo.com");
    }
}