using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Handler for reset password command
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Handles the reset password command
    /// </summary>
    /// <param name="request">Reset password command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reset password result</returns>
    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing password reset request for email {Email}", request.Email);

            // Find user by email
            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed: User with email {Email} not found", request.Email);
                return ResetPasswordResult.Failed("Invalid reset code or email");
            }

            // Find password reset token
            var resetToken = await _passwordResetTokenRepository.GetByUserIdAndCodeAsync(user.Id, request.Code);
            if (resetToken == null)
            {
                _logger.LogWarning("Password reset failed: Invalid reset code for user {UserId}", user.Id);
                return ResetPasswordResult.Failed("Invalid reset code or email");
            }

            // Validate token
            if (!resetToken.IsValid())
            {
                _logger.LogWarning("Password reset failed: Token is expired or already used for user {UserId}", user.Id);
                return ResetPasswordResult.Failed("Reset code has expired or has already been used");
            }

            // Validate the code matches
            if (!resetToken.ValidateCode(request.Code))
            {
                _logger.LogWarning("Password reset failed: Code mismatch for user {UserId}", user.Id);
                return ResetPasswordResult.Failed("Invalid reset code or email");
            }

            // Hash new password
            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            var newHashedPassword = new HashedPassword(newPasswordHash);

            // Update user password
            user.UpdatePassword(newHashedPassword);
            await _userRepository.UpdateAsync(user);

            // Mark token as used
            resetToken.MarkAsUsed();
            await _passwordResetTokenRepository.UpdateAsync(resetToken);

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);
            return ResetPasswordResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", request.Email);
            return ResetPasswordResult.Failed("An error occurred while resetting password");
        }
    }
}