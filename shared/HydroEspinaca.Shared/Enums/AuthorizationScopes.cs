namespace HydroEspinaca.Shared.Enums;

/// <summary>
/// Defines all available authorization scopes in the HydroEspinaca system.
/// These constants represent the canonical scopes used across all microservices.
/// </summary>
public static class AuthorizationScopes
{
    // User management scopes
    public const string UserRead = "user:read";
    public const string UserCreate = "user:create";
    public const string UserUpdate = "user:update";
    public const string UserDelete = "user:delete";

    // Profile scopes (for self-service operations)
    public const string ProfileRead = "profile:read";
    public const string ProfileUpdate = "profile:update";
    
    // Password management scopes
    public const string PasswordChange = "password:change";
    public const string PasswordReset = "password:reset";
    public const string PasswordResetRequest = "password:reset:request";

    // Role management scopes
    public const string RoleRead = "role:read";
    public const string RoleCreate = "role:create";
    public const string RoleUpdate = "role:update";
    public const string RoleDelete = "role:delete";

    // Permission management scopes
    public const string PermissionRead = "permission:read";
    public const string PermissionCreate = "permission:create";
    public const string PermissionUpdate = "permission:update";
    public const string PermissionDelete = "permission:delete";

    // Machine-to-Machine (M2M) scopes for IoT/Hardware services
    public const string SensorRead = "sensor:read";
    public const string SensorCreate = "sensor:create";
    public const string SensorUpdate = "sensor:update";
    public const string SensorDelete = "sensor:delete";
    public const string SensorWrite = "sensor:write"; // Legacy - for bulk operations

    // Reading management scopes
    public const string ReadingRead = "reading:read";
    public const string ReadingCreate = "reading:create";

    // Aggregate data scopes
    public const string AggregateRead = "aggregate:read";

    // ESP32 Alert management scopes
    public const string Esp32AlertRead = "esp32alert:read";
    public const string Esp32AlertWrite = "esp32alert:write";

    public const string ActuatorRead = "actuator:read";
    public const string ActuatorCreate = "actuator:create";
    public const string ActuatorUpdate = "actuator:update";
    public const string ActuatorDelete = "actuator:delete";
    public const string ActuatorControl = "actuator:control";

    // Command management scopes
    public const string CommandRead = "command:read";
    public const string CommandCreate = "command:create";

    // ESP32 Node management scopes
    public const string Esp32Read = "esp32:read";
    public const string Esp32Write = "esp32:write";
    public const string Esp32Control = "esp32:control";

    // Variable management scopes
    public const string VariableRead = "variable:read";
    public const string VariableWrite = "variable:write";

    // Alert management scopes
    public const string AlertRead = "alert:read";
    public const string AlertWrite = "alert:write";
    public const string AlertManage = "alert:manage";

    // Notification management scopes
    public const string NotificationSend = "notification:send";
    public const string NotificationRead = "notification:read";
    public const string NotificationManage = "notification:manage";
    public const string NotificationDiagnostics = "notification:diagnostics";

    

    // En AuthorizationScopes.cs - faltan estos scopes:
    public const string FuzzySystemRead = "fuzzy:system:read";
    public const string FuzzySystemCreate = "fuzzy:system:create";
    public const string FuzzySystemUpdate = "fuzzy:system:update";
    public const string FuzzySystemDelete = "fuzzy:system:delete";

    public const string FuzzyVariableRead = "fuzzy:variable:read";
    public const string FuzzyVariableCreate = "fuzzy:variable:create";
    public const string FuzzyVariableUpdate = "fuzzy:variable:update";
    public const string FuzzyVariableDelete = "fuzzy:variable:delete";

    public const string FuzzyEvaluationRead = "fuzzy:evaluation:read";
    public const string FuzzyEvaluationCreate = "fuzzy:evaluation:create";

    // Weather scopes
    public const string WeatherRead = "weather:read";
    public const string WeatherWrite = "weather:write";

    // BI scopes
    public const string BiRead = "bi:read";
    public const string BiWrite = "bi:write";

    // System-level scopes
    public const string SystemAdmin = "system:admin";
    public const string SystemHealth = "system:health";
    public const string SystemMonitor = "system:monitor";

    /// <summary>
    /// Gets all user-related scopes
    /// </summary>
    public static readonly string[] UserScopes =
    {
        UserRead, UserCreate, UserUpdate, UserDelete,
        ProfileRead, ProfileUpdate,
        PasswordChange, PasswordReset, PasswordResetRequest
    };

    /// <summary>
    /// Gets all role management scopes
    /// </summary>
    public static readonly string[] RoleScopes =
    {
        RoleRead, RoleCreate, RoleUpdate, RoleDelete
    };

    /// <summary>
    /// Gets all permission management scopes
    /// </summary>
    public static readonly string[] PermissionScopes =
    {
        PermissionRead, PermissionCreate, PermissionUpdate, PermissionDelete
    };

    /// <summary>
    /// Gets all IoT hardware-related scopes
    /// </summary>
    public static readonly string[] HardwareScopes =
    {
        SensorRead, SensorCreate, SensorUpdate, SensorDelete, SensorWrite,
        ReadingRead, ReadingCreate,
        AggregateRead,
        Esp32AlertRead, Esp32AlertWrite,
        ActuatorRead, ActuatorCreate, ActuatorUpdate, ActuatorDelete, ActuatorControl,
        CommandRead, CommandCreate,
        Esp32Read, Esp32Write, Esp32Control
    };

    /// <summary>
    /// Gets all notification-related scopes
    /// </summary>
    public static readonly string[] NotificationScopes =
    {
        NotificationSend, NotificationRead, NotificationManage, NotificationDiagnostics
    };

    /// <summary>
    /// Gets all fuzzy logic system scopes
    /// </summary>
    public static readonly string[] FuzzyLogicScopes =
    {
        FuzzySystemRead, FuzzySystemCreate, FuzzySystemUpdate, FuzzySystemDelete,
        FuzzyVariableRead, FuzzyVariableCreate, FuzzyVariableUpdate, FuzzyVariableDelete,
        FuzzyEvaluationRead, FuzzyEvaluationCreate
    };

    /// <summary>
    /// Gets all monitoring and data management scopes
    /// </summary>
    public static readonly string[] MonitoringScopes =
    {
        VariableRead, VariableWrite,
        AlertRead, AlertWrite, AlertManage,
        SystemHealth, SystemMonitor
    };

    /// <summary>
    /// Gets all weather-related scopes
    /// </summary>
    public static readonly string[] WeatherScopes =
    {
        WeatherRead, WeatherWrite
    };

    /// <summary>
    /// Gets all BI scopes
    /// </summary>
    public static readonly string[] BiScopes =
    {
        BiRead, BiWrite
    };


    /// <summary>
    /// Gets all system-level scopes
    /// </summary>
    public static readonly string[] SystemScopes =
    {
        SystemAdmin, SystemHealth, SystemMonitor
    };

    /// <summary>
    /// Gets all available scopes in the system
    /// </summary>
    public static readonly string[] AllScopes =
    {
        // User management
        UserRead, UserCreate, UserUpdate, UserDelete,
        ProfileRead, ProfileUpdate,
        PasswordChange, PasswordReset, PasswordResetRequest,
        // Role and permission management
        RoleRead, RoleCreate, RoleUpdate, RoleDelete,
        PermissionRead, PermissionCreate, PermissionUpdate, PermissionDelete,
        // IoT Hardware
        SensorRead, SensorCreate, SensorUpdate, SensorDelete, SensorWrite,
        ReadingRead, ReadingCreate,
        AggregateRead,
        Esp32AlertRead, Esp32AlertWrite,
        ActuatorRead, ActuatorCreate, ActuatorUpdate, ActuatorDelete, ActuatorControl,
        CommandRead, CommandCreate,
        Esp32Read, Esp32Write, Esp32Control,
        // Data and monitoring
        VariableRead, VariableWrite,
        AlertRead, AlertWrite, AlertManage,
        // Notifications
        NotificationSend, NotificationRead, NotificationManage, NotificationDiagnostics,

        // Fuzzy logic system
        FuzzySystemRead, FuzzySystemCreate, FuzzySystemUpdate, FuzzySystemDelete,
        FuzzyVariableRead, FuzzyVariableCreate, FuzzyVariableUpdate, FuzzyVariableDelete,
        FuzzyEvaluationRead, FuzzyEvaluationCreate,

        // Weather
        WeatherRead, WeatherWrite,

        // BI
        BiRead, BiWrite,
        
        // System
        SystemAdmin, SystemHealth, SystemMonitor
    };
}