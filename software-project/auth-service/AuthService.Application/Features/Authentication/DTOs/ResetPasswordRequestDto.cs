namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for reset password request
/// </summary>
public class ResetPasswordRequestDto
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