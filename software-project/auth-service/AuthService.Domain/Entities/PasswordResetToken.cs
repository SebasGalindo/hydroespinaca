using HydroEspinaca.Shared.Abstractions;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a password reset token for temporary password reset functionality
/// </summary>
public class PasswordResetToken : IIdentifiableMutable
{
    /// <summary>
    /// Unique identifier for the password reset token
    /// </summary>
    public string Id { get; private set; }
    
    /// <summary>
    /// Reference to the user requesting password reset
    /// </summary>
    public string UserId { get; private set; }
    
    /// <summary>
    /// The temporary reset code (6-digit alphanumeric)
    /// </summary>
    public string Code { get; private set; }
    
    /// <summary>
    /// When the token expires (typically 10-15 minutes from creation)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// Whether the token has been used
    /// </summary>
    public bool IsUsed { get; private set; }
    
    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Parameterless constructor for serialization
    private PasswordResetToken() { }

    /// <summary>
    /// Creates a new password reset token
    /// </summary>
    /// <param name="userId">User ID requesting password reset</param>
    /// <param name="code">6-digit alphanumeric reset code</param>
    /// <param name="expiresAt">When the token expires</param>
    public PasswordResetToken(string userId, string code, DateTime expiresAt)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code cannot be null or empty", nameof(code));
        
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Code = code;
        ExpiresAt = expiresAt;
        IsUsed = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the token as used
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Token is already used");
        
        if (IsExpired())
            throw new InvalidOperationException("Cannot use an expired token");

        IsUsed = true;
    }

    /// <summary>
    /// Checks if the token is expired
    /// </summary>
    /// <returns>True if the token is expired</returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Validates if the token is valid for use
    /// </summary>
    /// <returns>True if the token can be used</returns>
    public bool IsValid()
    {
        return !IsUsed && !IsExpired();
    }

    /// <summary>
    /// Validates the provided code matches this token's code
    /// </summary>
    /// <param name="providedCode">Code to validate</param>
    /// <returns>True if codes match</returns>
    public bool ValidateCode(string providedCode)
    {
        return string.Equals(Code, providedCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sets the ID of the password reset token (required by IIdentifiableMutable)
    /// </summary>
    /// <param name="id">The ID to set</param>
    public void SetId(string id)
    {
        Id = id;
    }
}