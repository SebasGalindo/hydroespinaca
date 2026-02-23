namespace ChatbotService.Application.DTOs;

/// <summary>
/// Response para un token de streaming SSE.
/// </summary>
public class StreamTokenEvent
{
    /// <summary>
    /// Fragmento de texto generado.
    /// </summary>
    public required string Text { get; set; }
}

/// <summary>
/// Response final del streaming SSE.
/// </summary>
public class StreamDoneEvent
{
    /// <summary>
    /// Tokens totales usados en la generación.
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Título auto-generado de la sesión (basado en la primera pregunta).
    /// </summary>
    public string? SessionTitle { get; set; }
}
