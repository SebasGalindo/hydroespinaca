namespace ChatbotService.Application.DTOs;

/// <summary>
/// Request para enviar un mensaje en una sesión de chat.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Texto del mensaje del usuario.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Filtros opcionales de contexto para la consulta RAG.
    /// </summary>
    public ContextFilters? ContextFilters { get; set; }
}

/// <summary>
/// Filtros opcionales para afinar la consulta de contexto en el RAG.
/// </summary>
public class ContextFilters
{
    /// <summary>
    /// ID del sistema fuzzy para filtrar la búsqueda vectorial (opcional).
    /// </summary>
    public string? FuzzySystemId { get; set; }

    /// <summary>
    /// Rango de horas para datos en vivo (sensores, evaluaciones).
    /// </summary>
    public int? TimeRangeHours { get; set; }
}
