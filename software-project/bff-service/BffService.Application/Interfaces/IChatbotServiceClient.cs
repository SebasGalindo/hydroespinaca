using BffService.Domain.DTOs.Chatbot;

namespace BffService.Application.Interfaces;

/// <summary>
/// Client for communicating with the chatbot-service microservice.
/// Handles chat sessions, message streaming (SSE), and RAG synchronization.
/// </summary>
public interface IChatbotServiceClient
{
    // ──────────────────────────────────────────────
    //  Chat Sessions
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a new empty chat session for the given user.
    /// </summary>
    /// <param name="accessToken">JWT access token for authorization.</param>
    /// <param name="userId">ID of the user creating the session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response containing the new session ID.</returns>
    Task<CreateSessionResponse> CreateSessionAsync(
        string accessToken, string userId, CancellationToken ct = default);

    /// <summary>
    /// Lists all chat sessions for a user (ordered by most recent first).
    /// </summary>
    /// <param name="accessToken">JWT access token for authorization.</param>
    /// <param name="userId">ID of the user whose sessions to list.</param>
    /// <param name="skip">Number of sessions to skip (pagination).</param>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of session summaries.</returns>
    Task<List<ChatSessionDto>> GetUserSessionsAsync(
        string accessToken, string userId, int skip = 0, int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the message history for a specific session.
    /// </summary>
    /// <param name="accessToken">JWT access token for authorization.</param>
    /// <param name="sessionId">ID of the session to retrieve messages from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of messages in the session.</returns>
    Task<List<ChatMessageDto>> GetSessionMessagesAsync(
        string accessToken, string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Deletes (archives) a chat session.
    /// </summary>
    /// <param name="accessToken">JWT access token for authorization.</param>
    /// <param name="sessionId">ID of the session to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteSessionAsync(
        string accessToken, string sessionId, CancellationToken ct = default);

    // ──────────────────────────────────────────────
    //  Chat Streaming (SSE)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Sends a message to the chatbot and returns the raw HTTP response stream (SSE).
    /// The caller is responsible for streaming this response to the frontend without buffering.
    /// Uses <see cref="HttpCompletionOption.ResponseHeadersRead"/> for true streaming.
    /// </summary>
    /// <param name="accessToken">JWT access token for authorization.</param>
    /// <param name="sessionId">ID of the active chat session.</param>
    /// <param name="request">The message and optional context filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw HttpResponseMessage with streaming body (must be disposed by caller).</returns>
    Task<HttpResponseMessage> StreamMessageAsync(
        string accessToken, string sessionId, SendMessageRequest request,
        CancellationToken ct = default);

    // ──────────────────────────────────────────────
    //  RAG Synchronization (M2M / internal)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Notifies chatbot-service of a change in a fuzzy entity for re-vectorization.
    /// Called as a fire-and-forget side-effect from FuzzyController mutations.
    /// </summary>
    /// <param name="accessToken">JWT access token with rag:manage scope.</param>
    /// <param name="request">Sync request with source type, ID, and action.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SyncKnowledgeAsync(
        string accessToken, SyncKnowledgeRequest request, CancellationToken ct = default);

    // ──────────────────────────────────────────────
    //  RAG Reindex (Admin / manual)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Triggers a full re-indexation of all fuzzy knowledge chunks in the chatbot RAG.
    /// This rebuilds vector embeddings for all systems, variables, terms, and rules.
    /// </summary>
    /// <param name="accessToken">JWT access token with rag:manage scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response with the total number of chunks indexed.</returns>
    Task<ReindexKnowledgeResponse> ReindexKnowledgeAsync(
        string accessToken, CancellationToken ct = default);
}
