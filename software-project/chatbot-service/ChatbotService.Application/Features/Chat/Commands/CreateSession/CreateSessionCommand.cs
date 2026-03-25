using ChatbotService.Application.DTOs;
using MediatR;

namespace ChatbotService.Application.Features.Chat.Commands.CreateSession;

/// <summary>
/// Comando para crear una nueva sesión de chat vacía.
/// </summary>
/// <param name="UserId">ID del usuario propietario.</param>
public record CreateSessionCommand(string UserId) : IRequest<CreateSessionResponse>;
