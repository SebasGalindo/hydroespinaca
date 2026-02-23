using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Domain.DTOs.Chatbot;
using BffService.Api.Helpers;

namespace BffService.Api.Controllers;

/// <summary>
/// Controller for proxying chat requests to chatbot-service.
/// Handles session management and SSE streaming passthrough.
/// Follows the same session-based auth pattern as FuzzyController.
/// </summary>
[ApiController]
[Route("chat")]
[AllowAnonymous] // Session is validated manually via BaseAuthenticatedController
public class ChatbotController : BaseAuthenticatedController
{
    private readonly IChatbotServiceClient _chatbotClient;

    public ChatbotController(
        IChatbotServiceClient chatbotClient,
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger<ChatbotController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _chatbotClient = chatbotClient;
    }

    // ──────────────────────────────────────────────
    //  Session Management
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new empty chat session for the logged-in user.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(CreateSessionResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateSession(CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _chatbotClient.CreateSessionAsync(
                session.AccessToken, session.UserId!, ct);
            return StatusCode(201, result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "creating chat session");
        }
    }

    /// <summary>
    /// Lists all chat sessions for the logged-in user (paginated).
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<ChatSessionDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _chatbotClient.GetUserSessionsAsync(
                session.AccessToken, session.UserId!, skip, limit, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "listing chat sessions");
        }
    }

    /// <summary>
    /// Gets the full message history for a specific session.
    /// </summary>
    [HttpGet("messages/{sessionId}")]
    [ProducesResponseType(typeof(List<ChatMessageDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetMessages(string sessionId, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            var result = await _chatbotClient.GetSessionMessagesAsync(
                session.AccessToken, sessionId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "getting chat messages", sessionId);
        }
    }

    /// <summary>
    /// Deletes (archives) a chat session.
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);
            await _chatbotClient.DeleteSessionAsync(
                session.AccessToken, sessionId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(
                ex, Logger, "deleting chat session", sessionId);
        }
    }

    // ──────────────────────────────────────────────
    //  Chat Streaming (SSE passthrough)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sends a message to the chatbot and streams the response via SSE.
    /// This endpoint does true passthrough — it reads chunks from chatbot-service
    /// and writes them to the client's response stream without buffering.
    /// </summary>
    [HttpPost("stream/{sessionId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task StreamMessage(
        string sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            var session = await ValidateSessionAsync(ct);

            // Get the streaming response from chatbot-service
            using var upstreamResponse = await _chatbotClient.StreamMessageAsync(
                session.AccessToken, sessionId, request, ct);

            // Set SSE headers on our response
            Response.StatusCode = 200;
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no"; // Prevent Nginx buffering

            // Passthrough: pipe upstream stream directly to client
            await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(upstreamStream);

            var buffer = new char[512];
            int bytesRead;

            while ((bytesRead = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await Response.Body.WriteAsync(
                    System.Text.Encoding.UTF8.GetBytes(buffer, 0, bytesRead), ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — normal for SSE
            Logger.LogDebug("Client disconnected from SSE stream for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error streaming chat response for session {SessionId}", sessionId);

            // If we haven't started writing the response body yet, we can return an error
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                Response.ContentType = "application/json";
                await Response.WriteAsJsonAsync(
                    new { message = "Error streaming chat response" }, ct);
            }
        }
    }
}
