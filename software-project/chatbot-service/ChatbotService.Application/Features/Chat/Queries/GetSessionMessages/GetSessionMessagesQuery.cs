using ChatbotService.Application.DTOs.Chat;
using MediatR;

namespace ChatbotService.Application.Features.Chat.Queries.GetSessionMessages;

/// <summary>
/// Query para obtener el historial de mensajes de una sesión específica.
/// </summary>
/// <param name="SessionId">ID de la sesión.</param>
/// <param name="UserId">ID del usuario (para validación de ownership).</param>
public record GetSessionMessagesQuery(string SessionId, string UserId) : IRequest<List<ChatMessageDto>>;
