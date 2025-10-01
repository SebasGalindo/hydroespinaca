namespace AuthService.Domain.Interfaces;

/// <summary>
/// Interface for generating secure codes
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates a secure 6-digit alphanumeric code
    /// </summary>
    /// <returns>6-character alphanumeric code</returns>
    string GenerateResetCode();
}