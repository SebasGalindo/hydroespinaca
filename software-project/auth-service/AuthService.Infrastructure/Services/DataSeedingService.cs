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
    private readonly IClientAppRepository _clientAppRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IClientAppRepository clientAppRepository,
        IPasswordHasher passwordHasher,
        ILogger<DataSeedingService> logger)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _clientAppRepository = clientAppRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        _logger.LogInformation("Starting data seeding for auth-service");
        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedM2MClientsAsync();
        await SeedAdminUserAsync();
        await SeedTestUserAsync();
        await VerifyDataIntegrityAsync();
        _logger.LogInformation("Data seeding completed successfully");
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
            var adminPasswordValid = _passwordHasher.Verify(adminUser.Password.Value, "dF^J`c'662:W");
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
            var userPasswordValid = _passwordHasher.Verify(testUser.Password.Value, "N16'+4a597|V!");
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

                var permissions = await _permissionRepository.FindByCodesAsync(role.Permissions);
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
            // User management permissions - Code es el scope exacto, Name es descriptivo
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.UserCreate, "User Create Permission", "Create new users"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, "User Read Permission", "Read user information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.UserUpdate, "User Update Permission", "Update user information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.UserDelete, "User Delete Permission", "Delete users"),
            
            // Profile management permissions (self-service)
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead, "Profile Read Permission", "Read own profile information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileUpdate, "Profile Update Permission", "Update own profile information"),
            
            // Password management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordChange, "Password Change Permission", "Change own password"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordReset, "Password Reset Permission", "Reset password using verification code"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordResetRequest, "Password Reset Request Permission", "Request password reset via email"),
            
            // Role management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleCreate, "Role Create Permission", "Create new roles"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, "Role Read Permission", "Read role information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleUpdate, "Role Update Permission", "Update role information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleDelete, "Role Delete Permission", "Delete roles"),
            
            // Permission management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionCreate, "Permission Create Permission", "Create new permissions"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead, "Permission Read Permission", "Read permission information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionUpdate, "Permission Update Permission", "Update permission information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionDelete, "Permission Delete Permission", "Delete permissions"),
            
            // === SENSOR-SERVICE PERMISSIONS ===
            // Sensor management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead, "Sensor Read Permission", "Read sensor data and information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorCreate, "Sensor Create Permission", "Create new sensors"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorUpdate, "Sensor Update Permission", "Update sensor configuration"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorDelete, "Sensor Delete Permission", "Delete sensors"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorWrite, "Sensor Write Permission", "Write sensor data and configuration"),
            
            // Reading management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ReadingRead, "Reading Read Permission", "Read sensor reading data"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ReadingCreate, "Reading Create Permission", "Create sensor readings"),
            
            // Aggregate data permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.AggregateRead, "Aggregate Read Permission", "Read aggregated sensor data"),
            
            // ESP32 Alert management permissions (sensor-service)
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32AlertRead, "ESP32 Alert Read Permission", "Read ESP32 alert information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32AlertWrite, "ESP32 Alert Write Permission", "Write and acknowledge ESP32 alerts"),
            
            // === ACTUATOR-SERVICE PERMISSIONS ===
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorRead, "Actuator Read Permission", "Read actuator status and information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorCreate, "Actuator Create Permission", "Create new actuators"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorUpdate, "Actuator Update Permission", "Update actuator configuration"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorDelete, "Actuator Delete Permission", "Delete actuators"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.ActuatorControl, "Actuator Control Permission", "Control actuator operations"),
            
            // Command management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.CommandRead, "Command Read Permission", "Read command information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.CommandCreate, "Command Create Permission", "Create new commands"),
            
            // ESP32 Node permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Read, "ESP32 Read Permission", "Read ESP32 node information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Write, "ESP32 Write Permission", "Write ESP32 node configuration"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.Esp32Control, "ESP32 Control Permission", "Control ESP32 node operations"),
            
            // Variable management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead, "Variable Read Permission", "Read variable information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableWrite, "Variable Write Permission", "Write variable data and configuration"),
            
            // Alert management permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertRead, "Alert Read Permission", "Read alert information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertWrite, "Alert Write Permission", "Write alert data and configuration"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.AlertManage, "Alert Manage Permission", "Manage alert rules and configuration"),
            
            // === NOTIFICATION-SERVICE PERMISSIONS ===
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend, "Notification Send Permission", "Send notifications"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationRead, "Notification Read Permission", "Read notification history"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationManage, "Notification Manage Permission", "Manage notification settings"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationDiagnostics, "Notification Diagnostics Permission", "Access notification diagnostics"),
            
            // === FUZZY-SERVICE PERMISSIONS ===
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzySystemRead, "Fuzzy System Read Permission", "Read fuzzy system configurations"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzySystemCreate, "Fuzzy System Create Permission", "Create new fuzzy systems"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzySystemUpdate, "Fuzzy System Update Permission", "Update fuzzy system configurations"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzySystemDelete, "Fuzzy System Delete Permission", "Delete fuzzy systems"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyVariableRead, "Fuzzy Variable Read Permission", "Read fuzzy variable configurations"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyVariableCreate, "Fuzzy Variable Create Permission", "Create new fuzzy variables"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyVariableUpdate, "Fuzzy Variable Update Permission", "Update fuzzy variable configurations"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyVariableDelete, "Fuzzy Variable Delete Permission", "Delete fuzzy variables"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyEvaluationRead, "Fuzzy Evaluation Read Permission", "Read fuzzy evaluation results"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.FuzzyEvaluationCreate, "Fuzzy Evaluation Create Permission", "Create fuzzy evaluations"),
            
            // System-level permissions
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin, "System Admin Permission", "System administration access"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth, "System Health Permission", "Access system health information"),
            new Permission(HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor, "System Monitor Permission", "Monitor system operations")
        };

        var createdCount = 0;
        var totalPermissions = permissions.Length;
        foreach (var permission in permissions)
        {
            var existing = await _permissionRepository.FindByCodeAsync(permission.Code);
            if (existing == null)
            {
                await _permissionRepository.CreateAsync(permission);
                createdCount++;
            }
        }
        
        if (createdCount > 0)
        {
            _logger.LogInformation("Created {CreatedCount} new permissions. Total: {TotalPermissions}", 
                createdCount, totalPermissions);
        }
    }

    private async Task SeedRolesAsync()
    {
        var adminPermissionCodes = new[]
        {
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin, 
        };

        var adminPermissions = await _permissionRepository.FindByCodesAsync(adminPermissionCodes);
        var adminPermissionCodesList = adminPermissions.Select(p => p.Code).ToList();

        if (adminPermissions.Count != adminPermissionCodes.Length)
        {
            var foundCodes = adminPermissions.Select(p => p.Code).ToHashSet();
            var missingCodes = adminPermissionCodes.Where(code => !foundCodes.Contains(code)).ToList();
            _logger.LogWarning("Missing admin permission codes: {MissingCodes}", string.Join(", ", missingCodes));
        }

        var adminRole = new Role(HydroEspinaca.Shared.Constants.SystemRoles.Admin, "Administrator", adminPermissionCodesList);
        var existingAdminRole = await _roleRepository.FindByCodeAsync(adminRole.Code);
        if (existingAdminRole == null)
        {
            await _roleRepository.CreateAsync(adminRole);
            _logger.LogInformation("Created Admin role with {PermissionCount} permissions", adminPermissionCodesList.Count);
        }

        var userPermissionCodes = new[]
        {
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileUpdate,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordChange,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordReset,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordResetRequest,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
        };

        var userPermissions = await _permissionRepository.FindByCodesAsync(userPermissionCodes);
        var userPermissionCodesList = userPermissions.Select(p => p.Code).ToList();

        if (userPermissions.Count != userPermissionCodes.Length)
        {
            var foundCodes = userPermissions.Select(p => p.Code).ToHashSet();
            var missingCodes = userPermissionCodes.Where(code => !foundCodes.Contains(code)).ToList();
            _logger.LogWarning("Missing user permission codes: {MissingCodes}", string.Join(", ", missingCodes));
        }

        var userRole = new Domain.Entities.Role(HydroEspinaca.Shared.Constants.SystemRoles.User, "User", userPermissionCodesList);
        var existingUserRole = await _roleRepository.FindByCodeAsync(userRole.Code);
        if (existingUserRole == null)
        {
            await _roleRepository.CreateAsync(userRole);
            _logger.LogInformation("Created User role with {PermissionCount} permissions", userPermissionCodesList.Count);
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@demo.com";
        const string adminPassword = "dF^J`c'662:W";

        var existingUser = await _userRepository.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            var adminRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.Admin);
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found, skipping admin user creation");
                return;
            }

            var hashedPassword = _passwordHasher.Hash(adminPassword);
            var adminUser = new User(
                "Administrador",
                new Email(adminEmail),
                new HashedPassword(hashedPassword),
                adminRole.Id
            );

            await _userRepository.CreateAsync(adminUser);
            _logger.LogInformation("Created admin user: {Email}", adminEmail);
        }
    }

    private async Task SeedTestUserAsync()
    {
        const string userEmail = "user@demo.com";
        const string userPassword = "N16'+4a597|V!";

        var existingUser = await _userRepository.FindByEmailAsync(userEmail);
        if (existingUser == null)
        {
            var userRole = await _roleRepository.FindByCodeAsync(HydroEspinaca.Shared.Constants.SystemRoles.User);
            if (userRole == null)
            {
                _logger.LogWarning("User role not found, skipping test user creation");
                return;
            }

            var hashedPassword = _passwordHasher.Hash(userPassword);
            var testUser = new User(
                "Usuario Demo",
                new Email(userEmail),
                new HashedPassword(hashedPassword),
                userRole.Id
            );

            await _userRepository.CreateAsync(testUser);
            _logger.LogInformation("Created test user: {Email}", userEmail);
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

    /// <summary>
    /// Seeds M2M (Machine-to-Machine) client applications for microservice authentication
    /// </summary>
    private async Task SeedM2MClientsAsync()
    {
        var m2mClients = new[]
        {
            new {
                Code = "bff-service-client",
                ClientId = "bff-service-m2m",
                ClientSecret = "2Qje57qfGWo9",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            },
            new {
                Code = "auth-service-client",
                ClientId = "auth-service-m2m",
                ClientSecret = "Arfmk2Fk7r4f",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            },
            new {
                Code = "sensor-service-client",
                ClientId = "sensor-service-m2m",
                ClientSecret = "sFv6IkmZX2V98",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
                }
            },
            new {
                Code = "actuator-service-client",
                ClientId = "actuator-service-m2m",
                ClientSecret = "lMag54vgU56x",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.ReadingRead,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
                }
            },
            new {
                Code = "notification-service-client",
                ClientId = "notification-service-m2m",
                ClientSecret = "Jl04aOK21mWk",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            },
            new {
                Code = "fuzzy-service-client",
                ClientId = "fuzzy-service-m2m",
                ClientSecret = "12RreUNF23Rc",
                Scopes = new[]
                {
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.CommandCreate,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            }
        };

        var createdCount = 0;
        foreach (var client in m2mClients)
        {
            var existing = await _clientAppRepository.FindByClientIdAsync(client.ClientId);
            if (existing == null)
            {
                var permissions = await _permissionRepository.FindByCodesAsync(client.Scopes);
                var permissionCodes = permissions.Select(p => p.Code).ToList();
                
                if (permissions.Count != client.Scopes.Length)
                {
                    var foundCodes = permissions.Select(p => p.Code).ToHashSet();
                    var missingCodes = client.Scopes.Where(code => !foundCodes.Contains(code)).ToList();
                    _logger.LogWarning("M2M Client {ClientId}: Missing permission codes: {MissingCodes}", 
                        client.ClientId, string.Join(", ", missingCodes));
                }
                
                var hashedSecret = _passwordHasher.Hash(client.ClientSecret);
                var clientApp = new ClientApp(
                    client.Code,
                    client.ClientId,
                    new HashedPassword(hashedSecret), 
                    permissionCodes
                );
                await _clientAppRepository.AddAsync(clientApp);
                createdCount++;
            }
        }
        
        if (createdCount > 0)
        {
            _logger.LogInformation("Created {CreatedCount} M2M clients", createdCount);
        }
    }
}