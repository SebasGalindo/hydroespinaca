using ChatbotService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Application.Features.Chat.Commands.DeleteSession;

/// <summary>
/// Handler que archiva (soft delete) una sesión de chat.
/// Valida que la sesión pertenezca al usuario solicitante.
/// </summary>
public class DeleteSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ILogger<DeleteSessionCommandHandler> logger)
    : IRequestHandler<DeleteSessionCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
        {
            logger.LogWarning("Sesión {SessionId} no encontrada para eliminación", request.SessionId);
            return false;
        }

        if (session.UserId != request.UserId)
        {
            logger.LogWarning("Usuario {UserId} intentó eliminar sesión {SessionId} que no le pertenece",
                request.UserId, request.SessionId);
            return false;
        }

        var result = await sessionRepository.DeleteAsync(request.SessionId, cancellationToken);
        logger.LogInformation("Sesión {SessionId} archivada: {Result}", request.SessionId, result);
        return result;
    }
}
