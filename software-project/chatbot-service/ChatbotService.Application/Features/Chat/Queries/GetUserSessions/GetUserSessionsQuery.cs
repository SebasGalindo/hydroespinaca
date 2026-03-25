using ChatbotService.Application.DTOs.Chat;
using MediatR;

namespace ChatbotService.Application.Features.Chat.Queries.GetUserSessions;

/// <summary>
/// Query para obtener las sesiones de chat de un usuario (paginadas, ordenadas desc por UpdatedAt).
/// </summary>
/// <param name="UserId">ID del usuario autenticado.</param>
/// <param name="Skip">Registros a omitir (paginación).</param>
/// <param name="Limit">Máximo de registros a retornar.</param>
public record GetUserSessionsQuery(string UserId, int Skip = 0, int Limit = 20) : IRequest<List<ChatSessionDto>>;
