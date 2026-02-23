using ChatbotService.Application.DTOs;
using ChatbotService.Domain.Entities;
using MediatR;

namespace ChatbotService.Application.Features.Chat.Commands.SendMessage;

/// <summary>
/// Comando principal del chatbot: envía un mensaje del usuario y retorna
/// un stream de tokens de la respuesta generada por el RAG pipeline.
/// </summary>
/// <param name="SessionId">ID de la sesión de chat activa.</param>
/// <param name="UserId">ID del usuario autenticado.</param>
/// <param name="Message">Texto del mensaje del usuario.</param>
/// <param name="ContextFilters">Filtros opcionales de contexto.</param>
public record SendMessageCommand(
    string SessionId,
    string UserId,
    string Message,
    ContextFilters? ContextFilters) : IRequest<IAsyncEnumerable<string>>;
