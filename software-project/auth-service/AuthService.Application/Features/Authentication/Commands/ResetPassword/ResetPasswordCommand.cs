using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Command to reset password using a verification code
/// </summary>
public class ResetPasswordCommand : IRequest<ResetPasswordResult>
{
    /// <summary>
    /// Email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit verification code received via email
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// New password to set
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Result of password reset operation
/// </summary>
public class ResetPasswordResult
{
    /// <summary>
    /// Whether the password reset was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ResetPasswordResult Successful(string message = "Password has been reset successfully") 
        => new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static ResetPasswordResult Failed(string errorMessage) => new() 
    { 
        Success = false, 
        ErrorMessage = errorMessage 
    };
}