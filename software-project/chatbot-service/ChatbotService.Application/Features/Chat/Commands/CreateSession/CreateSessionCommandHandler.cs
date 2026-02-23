using ChatbotService.Application.DTOs;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Chat.Commands.CreateSession;

/// <summary>
/// Handler que crea una nueva sesión de chat vacía para un usuario.
/// </summary>
public class CreateSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<CreateSessionCommandHandler> logger)
    : IRequestHandler<CreateSessionCommand, CreateSessionResponse>
{
    /// <inheritdoc />
    public async Task<CreateSessionResponse> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new ChatSession
        {
            UserId = request.UserId,
            Title = "Nueva conversación"
        };
        session.SetId(Guid.NewGuid().ToString());

        await sessionRepository.CreateAsync(session, cancellationToken);

        logger.LogInformation("Sesión de chat creada: {SessionId} para usuario {UserId}", session.Id, request.UserId);

        return new CreateSessionResponse { SessionId = session.Id };
    }
}
