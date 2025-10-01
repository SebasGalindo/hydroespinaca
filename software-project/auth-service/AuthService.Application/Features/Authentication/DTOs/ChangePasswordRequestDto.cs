namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for change password request
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// User's current password for validation
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password to set
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}