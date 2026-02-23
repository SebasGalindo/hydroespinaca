namespace ChatbotService.Application.DTOs.Chat;

/// <summary>
/// DTO de sesión de chat expuesto en la API (sin datos internos como UserId o IsArchived).
/// </summary>
/// <param name="Id">Identificador único de la sesión.</param>
/// <param name="Title">Título auto-generado.</param>
/// <param name="CreatedAt">Fecha de creación.</param>
/// <param name="UpdatedAt">Fecha de la última actividad.</param>
public record ChatSessionDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
