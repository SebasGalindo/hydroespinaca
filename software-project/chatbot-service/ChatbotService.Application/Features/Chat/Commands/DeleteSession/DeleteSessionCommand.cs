using MediatR;

namespace ChatbotService.Application.Features.Chat.Commands.DeleteSession;

/// <summary>
/// Comando para eliminar (archivar) una sesión de chat.
/// </summary>
/// <param name="SessionId">ID de la sesión a eliminar.</param>
/// <param name="UserId">ID del usuario propietario (para validación de ownership).</param>
public record DeleteSessionCommand(string SessionId, string UserId) : IRequest<bool>;
