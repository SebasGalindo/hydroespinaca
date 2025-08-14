using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public class DataSeedingService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<DataSeedingService> logger)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedAdminUserAsync();
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

        foreach (var permission in permissions)
        {
            var existing = await _permissionRepository.FindByCodeAsync(permission.Code);
            if (existing == null)
            {
                await _permissionRepository.CreateAsync(permission);
                _logger.LogInformation("Created permission: {PermissionCode}", permission.Code);
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

        // Resolve permission codes to ObjectIds
        var permissions = await _permissionRepository.FindByCodesAsync(adminPermissionCodes);
        var permissionIds = permissions.Select(p => p.Id).ToList();

        var adminRole = new Domain.Entities.Role("role_admin", "Administrator", permissionIds);

        var existing = await _roleRepository.FindByCodeAsync(adminRole.Code);
        if (existing == null)
        {
            await _roleRepository.CreateAsync(adminRole);
            _logger.LogInformation("Created admin role: {RoleCode}", adminRole.Code);
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@demo.com";
        const string adminPassword = "Admin123!";

        var existingUser = await _userRepository.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            // Resolve role code to ObjectId
            var adminRole = await _roleRepository.FindByCodeAsync("role_admin");
            if (adminRole == null)
            {
                _logger.LogError("Admin role not found during user seeding");
                return;
            }

            var hashedPassword = _passwordHasher.Hash(adminPassword);
            var adminUser = new User(
                new Email(adminEmail),
                new HashedPassword(hashedPassword),
                adminRole.Id
            );

            await _userRepository.CreateAsync(adminUser);
            _logger.LogInformation("Created admin user: {Email}", adminEmail);
        }
    }
}