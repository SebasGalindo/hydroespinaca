using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Authentication.Commands.ChangePassword;

/// <summary>
/// Handler for change password command
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Handles the change password command
    /// </summary>
    /// <param name="request">Change password command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Change password result</returns>
    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing password change request for user {UserId}", request.UserId);

            // Get user by ID
            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed: User {UserId} not found", request.UserId);
                return ChangePasswordResult.Failed("User not found");
            }

            // Verify current password
            if (!_passwordHasher.Verify(user.Password.Value, request.OldPassword))
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", request.UserId);
                return ChangePasswordResult.Failed("Current password is incorrect");
            }

            // Hash new password
            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            var newHashedPassword = new HashedPassword(newPasswordHash);

            // Update user password
            user.UpdatePassword(newHashedPassword);

            // Save changes
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password successfully changed for user {UserId}", request.UserId);
            return ChangePasswordResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return ChangePasswordResult.Failed("An error occurred while changing password");
        }
    }
}