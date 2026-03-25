using ChatbotService.Application.DTOs.Chat;
using ChatbotService.Domain.Interfaces;
using MediatR;

namespace ChatbotService.Application.Features.Chat.Queries.GetUserSessions;

/// <summary>
/// Handler que obtiene las sesiones de chat de un usuario paginadas.
/// </summary>
public class GetUserSessionsQueryHandler(IChatSessionRepository sessionRepository)
    : IRequestHandler<GetUserSessionsQuery, List<ChatSessionDto>>
{
    /// <inheritdoc />
    public async Task<List<ChatSessionDto>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await sessionRepository.GetSessionsByUserAsync(
            request.UserId, request.Skip, request.Limit, cancellationToken);

        return sessions
            .Select(s => new ChatSessionDto(s.Id, s.Title, s.CreatedAt, s.UpdatedAt))
            .ToList();
    }
}
