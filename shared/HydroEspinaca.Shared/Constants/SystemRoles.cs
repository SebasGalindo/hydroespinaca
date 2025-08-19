namespace HydroEspinaca.Shared.Constants;

/// <summary>
/// Defines system role constants used across the HydroEspinaca system
/// </summary>
public static class SystemRoles
{
    /// <summary>
    /// Administrator role with full system access
    /// </summary>
    public const string Admin = "role_admin";

    /// <summary>
    /// Regular user role with limited access
    /// </summary>
    public const string User = "role_user";

    /// <summary>
    /// Client role for machine-to-machine authentication
    /// </summary>
    public const string Client = "client";
}