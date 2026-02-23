namespace ChatbotService.Application.DTOs.Chat;

/// <summary>
/// DTO de mensaje de chat expuesto en la API.
/// </summary>
/// <param name="Role">Rol de quien envió el mensaje (user, model, system).</param>
/// <param name="Content">Contenido de texto del mensaje.</param>
/// <param name="Timestamp">Fecha y hora del envío.</param>
/// <param name="TokensUsed">Tokens consumidos (solo para rol 'model').</param>
public record ChatMessageDto(
    string Role,
    string Content,
    DateTime Timestamp,
    int? TokensUsed
);
