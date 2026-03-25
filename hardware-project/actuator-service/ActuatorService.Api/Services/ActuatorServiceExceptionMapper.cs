using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace ActuatorService.Api.Services;

/// <summary>
/// Maps domain and application exceptions to appropriate HTTP status codes and error responses.
/// </summary>
public class ActuatorServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    public ActuatorServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        return exception switch
        {
            // Add actuator service-specific exceptions here if needed
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    public bool CanHandle(Exception exception) => _sharedMapper.CanHandle(exception);
    public int GetStatusCode(Exception exception) => _sharedMapper.GetStatusCode(exception);
}