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


    public DataSeedingService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IClientAppRepository clientAppRepository,
        IPasswordHasher passwordHasher)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _clientAppRepository = clientAppRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedM2MClientsAsync();
            await SeedAdminUserAsync();
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
        Console.WriteLine($"🛠️  Permissions: Created {createdCount} new permissions, {totalPermissions - createdCount} already existed. Total: {totalPermissions}");
    }

    private async Task SeedRolesAsync()
    {
        // Admin role with full management permissions - usar los CODES (scopes)
        var adminPermissionCodes = new[]
        {
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemAdmin, 
        };

        var adminPermissions = await _permissionRepository.FindByCodesAsync(adminPermissionCodes);
        var adminPermissionIds = adminPermissions.Select(p => p.Id).ToList();

        // Debug: Verificar que se encontraron los permisos
        Console.WriteLine($"🔍 Admin role: Found {adminPermissions.Count} permissions out of {adminPermissionCodes.Length} requested");
        if (adminPermissions.Count != adminPermissionCodes.Length)
        {
            var foundCodes = adminPermissions.Select(p => p.Code).ToHashSet();
            var missingCodes = adminPermissionCodes.Where(code => !foundCodes.Contains(code)).ToList();
            Console.WriteLine($"⚠️  Missing permission codes: {string.Join(", ", missingCodes)}");
        }

        var adminRole = new Role(HydroEspinaca.Shared.Constants.SystemRoles.Admin, "Administrator", adminPermissionIds);
        var existingAdminRole = await _roleRepository.FindByCodeAsync(adminRole.Code);
        if (existingAdminRole == null)
        {
            await _roleRepository.CreateAsync(adminRole);
            Console.WriteLine($"✅ Created Admin role with {adminPermissionIds.Count} permissions");
        }
        else
        {
            Console.WriteLine($"ℹ️  Admin role already exists with {existingAdminRole.Permissions.Count()} permissions");
        }

        // Regular user role with basic read permissions
        var userPermissionCodes = new[]
        {
            // Basic read permissions
            HydroEspinaca.Shared.Enums.AuthorizationScopes.UserRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.RoleRead, 
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PermissionRead,
            // Profile and password management (self-service)
            HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileRead,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.ProfileUpdate,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordChange,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordReset,
            HydroEspinaca.Shared.Enums.AuthorizationScopes.PasswordResetRequest,
            // System health for basic users
            HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
        };

        var userPermissions = await _permissionRepository.FindByCodesAsync(userPermissionCodes);
        var userPermissionIds = userPermissions.Select(p => p.Id).ToList();

        // Debug: Verificar que se encontraron los permisos de usuario
        Console.WriteLine($"🔍 User role: Found {userPermissions.Count} permissions out of {userPermissionCodes.Length} requested");
        if (userPermissions.Count != userPermissionCodes.Length)
        {
            var foundCodes = userPermissions.Select(p => p.Code).ToHashSet();
            var missingCodes = userPermissionCodes.Where(code => !foundCodes.Contains(code)).ToList();
            Console.WriteLine($"⚠️  Missing permission codes: {string.Join(", ", missingCodes)}");
        }

        var userRole = new Domain.Entities.Role(HydroEspinaca.Shared.Constants.SystemRoles.User, "User", userPermissionIds);
        var existingUserRole = await _roleRepository.FindByCodeAsync(userRole.Code);
        if (existingUserRole == null)
        {
            await _roleRepository.CreateAsync(userRole);
            Console.WriteLine($"✅ Created User role with {userPermissionIds.Count} permissions");
        }
        else
        {
            Console.WriteLine($"ℹ️  User role already exists with {existingUserRole.Permissions.Count()} permissions");
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

    /// <summary>
    /// Seeds M2M (Machine-to-Machine) client applications for microservice authentication
    /// </summary>
    private async Task SeedM2MClientsAsync()
    {
        var m2mClients = new[]
        {
            // === BFF-SERVICE M2M CLIENT ===
            // BFF solo necesita acceso a otros microservicios para proxy (no scopes propios de BFF)
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
            
            // === AUTH-SERVICE M2M CLIENT ===
            // Auth-service solo necesita comunicarse con otros servicios (no scopes propios de auth)
            new {
                Code = "auth-service-client",
                ClientId = "auth-service-m2m",
                ClientSecret = "Arfmk2Fk7r4f",
                Scopes = new[]
                {
                    // NOTIFICATION-SERVICE: Para enviar notificaciones de seguridad
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    // System health de otros servicios
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            },
            
            // === SENSOR-SERVICE M2M CLIENT ===
            // Sensor-service solo necesita acceso a otros servicios (no scopes propios de sensor)
            new {
                Code = "sensor-service-client",
                ClientId = "sensor-service-m2m",
                ClientSecret = "sFv6IkmZX2V98",
                Scopes = new[]
                {
                    // NOTIFICATION-SERVICE: Para envío de alertas críticas
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    // System monitoring de otros servicios
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
                }
            },
            
            // === ACTUATOR-SERVICE M2M CLIENT ===
            // Actuator-service solo necesita acceso a otros servicios (no scopes propios de actuator/command/esp32)
            new {
                Code = "actuator-service-client",
                ClientId = "actuator-service-m2m",
                ClientSecret = "lMag54vgU56x",
                Scopes = new[]
                {
                    // SENSOR-SERVICE: Para leer datos de sensores antes de actuar
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SensorRead,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.ReadingRead,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.VariableRead,
                    // NOTIFICATION-SERVICE: Para notificaciones de status
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.NotificationSend,
                    // System monitoring de otros servicios
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth
                }
            },
            
            // === NOTIFICATION-SERVICE M2M CLIENT ===
            // Notification-service solo necesita acceso a otros servicios (no scopes propios de notification)
            new {
                Code = "notification-service-client",
                ClientId = "notification-service-m2m",
                ClientSecret = "Jl04aOK21mWk",
                Scopes = new[]
                {
                    // System monitoring de otros servicios
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            },
            
            // === FUZZY-SERVICE M2M CLIENT ===
            // Fuzzy-service solo necesita acceso a otros servicios (no scopes propios de fuzzy)
            new {
                Code = "fuzzy-service-client",
                ClientId = "fuzzy-service-m2m",
                ClientSecret = "12RreUNF23Rc",
                Scopes = new[]
                {
                    // ACTUATOR-SERVICE: Para control basado en lógica fuzzy
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.CommandCreate,
                    // System monitoring de otros servicios
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemHealth,
                    HydroEspinaca.Shared.Enums.AuthorizationScopes.SystemMonitor
                }
            }
        };

        foreach (var client in m2mClients)
        {
            var existing = await _clientAppRepository.FindByClientIdAsync(client.ClientId);
            if (existing == null)
            {
                // Buscar permission IDs por sus codes (scopes)
                var permissions = await _permissionRepository.FindByCodesAsync(client.Scopes);
                var permissionIds = permissions.Select(p => p.Id).ToList();
                
                // Debug: Verificar que se encontraron los permisos
                Console.WriteLine($"🔍 M2M Client {client.ClientId}: Found {permissions.Count} permissions out of {client.Scopes.Length} requested");
                if (permissions.Count != client.Scopes.Length)
                {
                    var foundCodes = permissions.Select(p => p.Code).ToHashSet();
                    var missingCodes = client.Scopes.Where(code => !foundCodes.Contains(code)).ToList();
                    Console.WriteLine($"⚠️  Missing permission codes for {client.ClientId}: {string.Join(", ", missingCodes)}");
                }
                
                var hashedSecret = _passwordHasher.Hash(client.ClientSecret);
                var clientApp = new ClientApp(
                    client.Code,      // Code - Identificador interno único
                    client.ClientId,  // ClientId - Para protocolo OAuth2
                    new HashedPassword(hashedSecret), 
                    permissionIds     // Usar IDs de permisos encontrados
                );
                await _clientAppRepository.AddAsync(clientApp);
                
                Console.WriteLine($"✅ Created M2M client: Code='{client.Code}', ClientId='{client.ClientId}' with {permissionIds.Count} permission IDs");
            }
            else
            {
                Console.WriteLine($"ℹ️  M2M client already exists: {client.ClientId} with {existing.Scopes.Count()} scopes");
            }
        }
    }
}