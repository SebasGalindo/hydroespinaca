using ChatbotService.Application.DTOs.Chat;
using ChatbotService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Chat.Queries.GetSessionMessages;

/// <summary>
/// Handler que obtiene el historial de mensajes de una sesión.
/// Valida ownership del usuario sobre la sesión.
/// </summary>
public class GetSessionMessagesQueryHandler(
    IChatSessionRepository sessionRepository,
    ILogger<GetSessionMessagesQueryHandler> logger)
    : IRequestHandler<GetSessionMessagesQuery, List<ChatMessageDto>>
{
    /// <inheritdoc />
    public async Task<List<ChatMessageDto>> Handle(GetSessionMessagesQuery request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Sesión {SessionId} no encontrada", request.SessionId);
            return [];
        }

        if (session.UserId != request.UserId)
        {
            logger.LogWarning("Usuario {UserId} intentó acceder a sesión {SessionId} ajena",
                request.UserId, request.SessionId);
            return [];
        }

        return session.Messages
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.Timestamp, m.TokensUsed))
            .ToList();
    }
}
