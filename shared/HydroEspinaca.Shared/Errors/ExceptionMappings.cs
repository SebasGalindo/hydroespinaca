using FluentValidation;

namespace HydroEspinaca.Shared.Errors;

/// <summary>
/// Static class containing exception type mappings and their corresponding HTTP status codes
/// </summary>
public static class ExceptionMappings
{
    /// <summary>
    /// Dictionary mapping exception types to their corresponding HTTP status codes
    /// </summary>
    public static readonly Dictionary<Type, int> StatusCodeMappings = new()
    {
        // Authentication and Authorization
        { typeof(UnauthorizedAccessException), 401 },
        
        // Domain exceptions
        { typeof(DomainException), 400 },
        
        // Validation
        { typeof(ValidationException), 400 },
        
        // Not found
        { typeof(NotFoundException), 404 },
        
        // Conflict
        { typeof(ConflictException), 409 },
        
        // Unprocessable entity
        { typeof(UnprocessableEntityException), 422 },
        
        // Service unavailable
        { typeof(ServiceUnavailableException), 502 },
        
        // Argument exceptions
        { typeof(ArgumentException), 400 },
        { typeof(ArgumentNullException), 400 },
        
        // Default fallback
        { typeof(Exception), 500 }
    };

    /// <summary>
    /// Dictionary mapping exception types to their RFC 7807 problem type URIs
    /// </summary>
    public static readonly Dictionary<Type, string> ProblemTypeMappings = new()
    {
        { typeof(UnauthorizedAccessException), "https://tools.ietf.org/html/rfc9110#section-15.5.2" },
        { typeof(DomainException), "https://tools.ietf.org/html/rfc9110#section-15.5.1" },
        { typeof(ValidationException), "https://tools.ietf.org/html/rfc9110#section-15.5.1" },
        { typeof(NotFoundException), "https://tools.ietf.org/html/rfc9110#section-15.5.5" },
        { typeof(ConflictException), "https://tools.ietf.org/html/rfc9110#section-15.5.8" },
        { typeof(UnprocessableEntityException), "https://tools.ietf.org/html/rfc9110#section-15.5.9" },
        { typeof(ServiceUnavailableException), "https://tools.ietf.org/html/rfc9110#section-15.6.3" },
        { typeof(ArgumentException), "https://tools.ietf.org/html/rfc9110#section-15.5.1" },
        { typeof(ArgumentNullException), "https://tools.ietf.org/html/rfc9110#section-15.5.1" },
        { typeof(Exception), "https://tools.ietf.org/html/rfc9110#section-15.6.1" }
    };

    /// <summary>
    /// Dictionary mapping exception types to their user-friendly titles
    /// </summary>
    public static readonly Dictionary<Type, string> TitleMappings = new()
    {
        { typeof(UnauthorizedAccessException), "Unauthorized" },
        { typeof(DomainException), "Domain Error" },
        { typeof(ValidationException), "Validation Error" },
        { typeof(NotFoundException), "Resource Not Found" },
        { typeof(ConflictException), "Conflict" },
        { typeof(UnprocessableEntityException), "Unprocessable Entity" },
        { typeof(ServiceUnavailableException), "Service Unavailable" },
        { typeof(ArgumentException), "Invalid Parameter" },
        { typeof(ArgumentNullException), "Invalid Parameter" },
        { typeof(Exception), "Internal Server Error" }
    };

    /// <summary>
    /// Gets the HTTP status code for the given exception type
    /// </summary>
    public static int GetStatusCode(Type exceptionType)
    {
        // Check for exact match first
        if (StatusCodeMappings.TryGetValue(exceptionType, out var statusCode))
            return statusCode;

        // Check for inheritance hierarchy
        foreach (var mapping in StatusCodeMappings)
        {
            if (mapping.Key.IsAssignableFrom(exceptionType))
                return mapping.Value;
        }

        // Default fallback
        return 500;
    }

    /// <summary>
    /// Gets the problem type URI for the given exception type
    /// </summary>
    public static string GetProblemType(Type exceptionType)
    {
        // Check for exact match first
        if (ProblemTypeMappings.TryGetValue(exceptionType, out var problemType))
            return problemType;

        // Check for inheritance hierarchy
        foreach (var mapping in ProblemTypeMappings)
        {
            if (mapping.Key.IsAssignableFrom(exceptionType))
                return mapping.Value;
        }

        // Default fallback
        return ProblemTypeMappings[typeof(Exception)];
    }

    /// <summary>
    /// Gets the title for the given exception type
    /// </summary>
    public static string GetTitle(Type exceptionType)
    {
        // Check for exact match first
        if (TitleMappings.TryGetValue(exceptionType, out var title))
            return title;

        // Check for inheritance hierarchy
        foreach (var mapping in TitleMappings)
        {
            if (mapping.Key.IsAssignableFrom(exceptionType))
                return mapping.Value;
        }

        // Default fallback
        return TitleMappings[typeof(Exception)];
    }
}