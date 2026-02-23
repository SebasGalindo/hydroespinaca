namespace ChatbotService.Application.DTOs;

/// <summary>
/// Response para la creación de una sesión de chat.
/// </summary>
public class CreateSessionResponse
{
    /// <summary>
    /// ID de la sesión creada.
    /// </summary>
    public required string SessionId { get; set; }
}
