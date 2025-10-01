using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ChangePassword;

/// <summary>
/// Command to change user password (requires current password validation)
/// </summary>
public class ChangePasswordCommand : IRequest<ChangePasswordResult>
{
    /// <summary>
    /// User's current password for validation
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password to set
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// User ID (typically comes from JWT token)
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of password change operation
/// </summary>
public class ChangePasswordResult
{
    /// <summary>
    /// Whether the password change was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ChangePasswordResult Successful() => new() { Success = true };

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static ChangePasswordResult Failed(string errorMessage) => new() 
    { 
        Success = false, 
        ErrorMessage = errorMessage 
    };
}