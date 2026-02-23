using System.Text.Json;
using ChatbotService.Application.DTOs;
using ChatbotService.Application.DTOs.Chat;
using ChatbotService.Application.Features.Chat.Commands.CreateSession;
using ChatbotService.Application.Features.Chat.Commands.DeleteSession;
using ChatbotService.Application.Features.Chat.Commands.SendMessage;
using ChatbotService.Application.Features.Chat.Queries.GetSessionMessages;
using ChatbotService.Application.Features.Chat.Queries.GetUserSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotService.Api.Controllers;

/// <summary>
/// Controlador REST + SSE para operaciones de chat.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(IMediator mediator, ILogger<ChatController> logger) : ControllerBase
{
    /// <summary>
    /// Crea una nueva sesión de chat vacía.
    /// </summary>
    /// <returns>El ID de la sesión creada.</returns>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSession(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new CreateSessionCommand(userId), ct);
        return Created($"api/chat/sessions/{result.SessionId}", result);
    }

    /// <summary>
    /// Lista las sesiones de chat del usuario autenticado (paginadas).
    /// El userId se extrae del token JWT, no de la URL.
    /// </summary>
    /// <param name="skip">Registros a omitir.</param>
    /// <param name="limit">Máximo de registros.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<ChatSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSessions([FromQuery] int skip = 0, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var sessions = await mediator.Send(new GetUserSessionsQuery(userId, skip, limit), ct);
        return Ok(sessions);
    }

    /// <summary>
    /// Obtiene el historial de mensajes de una sesión.
    /// </summary>
    /// <param name="sessionId">ID de la sesión.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("sessions/{sessionId}/messages")]
    [ProducesResponseType(typeof(List<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionMessages(string sessionId, CancellationToken ct)
    {
        var userId = GetUserId();
        var messages = await mediator.Send(new GetSessionMessagesQuery(sessionId, userId), ct);
        return Ok(messages);
    }

    /// <summary>
    /// Elimina (archiva) una sesión de chat.
    /// </summary>
    /// <param name="sessionId">ID de la sesión a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await mediator.Send(new DeleteSessionCommand(sessionId, userId), ct);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// [SSE] Envía un mensaje del usuario y retorna un stream de la respuesta del chatbot.
    /// Implementa Server-Sent Events para streaming en tiempo real.
    /// </summary>
    /// <param name="sessionId">ID de la sesión activa.</param>
    /// <param name="request">Mensaje del usuario y filtros opcionales.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("stream/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task StreamMessage(string sessionId, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var totalTokens = 0;
        string? sessionTitle = null;

        try
        {
            var command = new SendMessageCommand(sessionId, userId, request.Message, request.ContextFilters);
            var tokenStream = await mediator.Send(command, ct);

            await foreach (var token in tokenStream.WithCancellation(ct))
            {
                totalTokens++;
                var tokenEvent = new StreamTokenEvent { Text = token };
                var json = JsonSerializer.Serialize(tokenEvent);

                await Response.WriteAsync($"event: token\ndata: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            // Evento final con metadata
            var doneEvent = new StreamDoneEvent { TokensUsed = totalTokens, SessionTitle = sessionTitle };
            var doneJson = JsonSerializer.Serialize(doneEvent);
            await Response.WriteAsync($"event: done\ndata: {doneJson}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante streaming de chat: sesión={SessionId}", sessionId);

            var errorJson = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"event: error\ndata: {errorJson}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    /// <summary>
    /// Extrae el UserId del claim JWT del usuario autenticado.
    /// </summary>
    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("No se pudo obtener el UserId del token.");
    }
}
