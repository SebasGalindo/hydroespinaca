using AuthService.Domain.Interfaces;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Service for generating secure codes
/// </summary>
public class CodeGenerator : ICodeGenerator
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    /// <summary>
    /// Generates a secure 6-digit alphanumeric code
    /// </summary>
    /// <returns>6-character alphanumeric code</returns>
    public string GenerateResetCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var chars = new char[CodeLength];
        var bytes = new byte[CodeLength];
        
        rng.GetBytes(bytes);
        
        for (int i = 0; i < CodeLength; i++)
        {
            chars[i] = AllowedChars[bytes[i] % AllowedChars.Length];
        }
        
        return new string(chars);
    }
}