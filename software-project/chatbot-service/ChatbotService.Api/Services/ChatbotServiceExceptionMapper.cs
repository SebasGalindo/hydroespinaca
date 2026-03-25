using HydroEspinaca.Shared.Authentication.Interfaces;
using HydroEspinaca.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotService.Api.Services;

/// <summary>
/// Mapper de excepciones específico del chatbot-service.
/// Delega al <see cref="ProblemDetailsFactory"/> compartido para excepciones comunes
/// y agrega mapeos específicos del servicio.
/// </summary>
public class ChatbotServiceExceptionMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsFactory _sharedMapper;

    /// <summary>
    /// Inicializa el mapper con la fábrica compartida.
    /// </summary>
    /// <param name="sharedMapper">Fábrica compartida de ProblemDetails.</param>
    public ChatbotServiceExceptionMapper(ProblemDetailsFactory sharedMapper)
    {
        _sharedMapper = sharedMapper;
    }

    /// <inheritdoc />
    public ProblemDetails MapToProblemDetails(Exception exception, string requestPath, bool isDevelopment)
    {
        return exception switch
        {
            InvalidOperationException ex => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Operación no válida",
                Status = 404,
                Detail = isDevelopment ? ex.Message : "Recurso no encontrado.",
                Instance = requestPath
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                Title = "No autorizado",
                Status = 403,
                Detail = "No tiene permisos para realizar esta operación.",
                Instance = requestPath
            },
            _ => _sharedMapper.MapToProblemDetails(exception, requestPath, isDevelopment)
        };
    }

    /// <inheritdoc />
    public bool CanHandle(Exception exception) => _sharedMapper.CanHandle(exception);

    /// <inheritdoc />
    public int GetStatusCode(Exception exception) => exception switch
    {
        InvalidOperationException => 404,
        UnauthorizedAccessException => 403,
        _ => _sharedMapper.GetStatusCode(exception)
    };
}
