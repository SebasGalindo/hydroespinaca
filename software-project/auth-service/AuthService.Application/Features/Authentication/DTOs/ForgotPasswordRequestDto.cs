namespace AuthService.Application.Features.Authentication.DTOs;

/// <summary>
/// DTO for forgot password request
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email address of the user requesting password reset
    /// </summary>
    public string Email { get; set; } = string.Empty;
}