using MediatR;

namespace AuthService.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Command to initiate password reset process by email
/// </summary>
public class ForgotPasswordCommand : IRequest<ForgotPasswordResult>
{
    /// <summary>
    /// Email address of the user requesting password reset
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Result of forgot password operation
/// </summary>
public class ForgotPasswordResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to user (should not reveal if email exists)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ForgotPasswordResult Successful(string message = "If the email exists, a password reset code has been sent") 
        => new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static ForgotPasswordResult Failed(string errorMessage) => new() 
    { 
        Success = false, 
        ErrorMessage = errorMessage 
    };
}