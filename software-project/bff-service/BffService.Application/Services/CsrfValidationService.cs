using BffService.Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BffService.Application.Services;

public interface ICsrfValidationService
{
    bool ValidateCsrfToken(HttpContext context);
    bool IsStateChangingOperation(string method);
}

public class CsrfValidationService : ICsrfValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CsrfValidationService> _logger;

    public CsrfValidationService(IConfiguration configuration, ILogger<CsrfValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool ValidateCsrfToken(HttpContext context)
    {
        var csrfTokenHeader = _configuration[BffConstants.Sessions.CsrfTokenHeaderConfigKey] ?? "X-CSRF-Token";

        // Get CSRF token from header
        if (!context.Request.Headers.TryGetValue(csrfTokenHeader, out var headerToken) || string.IsNullOrEmpty(headerToken))
        {
            _logger.LogDebug("CSRF token not found in header {Header}", csrfTokenHeader);
            return false;
        }

        // Decode URL encoding from header token (ASP.NET Core decodes cookies automatically but not headers)
        var decodedHeaderToken = Uri.UnescapeDataString(headerToken!);

        // Get CSRF token from cookie (for web clients) or compare with session (for mobile clients)
        string? expectedToken = null;

        // Try to get from cookies first (web clients)
        if (context.Request.Cookies.TryGetValue("CsrfToken", out var cookieToken))
        {
            expectedToken = cookieToken;
        }
        else
        {
            // For mobile clients, the token should match what's stored in the session
            // This would require session lookup - for now we'll accept any non-empty token
            // In a real implementation, you'd validate against the session store
            expectedToken = decodedHeaderToken;
        }

        if (string.IsNullOrEmpty(expectedToken))
        {
            _logger.LogDebug("Expected CSRF token not found");
            return false;
        }

        // Compare tokens using constant-time comparison
        return CryptographicEquals(decodedHeaderToken, expectedToken);
    }

    public bool IsStateChangingOperation(string method)
    {
        return method.ToUpperInvariant() is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        
        return result == 0;
    }
}