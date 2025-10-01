using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

/// <summary>
/// Repository interface for password reset token operations
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Creates a new password reset token
    /// </summary>
    /// <param name="token">Password reset token to create</param>
    /// <returns>Task representing the async operation</returns>
    Task CreateAsync(PasswordResetToken token);

    /// <summary>
    /// Gets an active (non-used, non-expired) password reset token for a user
    /// </summary>
    /// <param name="userId">User ID to search for</param>
    /// <returns>Active password reset token or null if none found</returns>
    Task<PasswordResetToken?> GetActiveByUserIdAsync(string userId);

    /// <summary>
    /// Gets a password reset token by its ID
    /// </summary>
    /// <param name="id">Token ID</param>
    /// <returns>Password reset token or null if not found</returns>
    Task<PasswordResetToken?> GetByIdAsync(string id);

    /// <summary>
    /// Updates an existing password reset token
    /// </summary>
    /// <param name="token">Token to update</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateAsync(PasswordResetToken token);

    /// <summary>
    /// Deletes expired password reset tokens
    /// </summary>
    /// <returns>Number of deleted tokens</returns>
    Task<int> DeleteExpiredAsync();

    /// <summary>
    /// Revokes (marks as used) all active tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of revoked tokens</returns>
    Task<int> RevokeActiveTokensForUserAsync(string userId);

    /// <summary>
    /// Gets a password reset token by user ID and code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="code">Reset code</param>
    /// <returns>Password reset token or null if not found</returns>
    Task<PasswordResetToken?> GetByUserIdAndCodeAsync(string userId, string code);
}