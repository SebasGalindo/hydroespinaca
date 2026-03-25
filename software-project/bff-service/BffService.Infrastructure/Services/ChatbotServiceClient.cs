using BffService.Application.Interfaces;
using BffService.Domain.DTOs.Chatbot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BffService.Infrastructure.Services;

/// <summary>
/// HTTP client for communicating with the chatbot-service microservice.
/// Follows the typed HttpClient pattern used by other BFF service clients.
/// Handles chat sessions, SSE streaming passthrough, and RAG sync notifications.
/// </summary>
public class ChatbotServiceClient : IChatbotServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatbotServiceClient> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatbotServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChatbotServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Services:ChatbotService:Url"] ?? "http://chatbot-service:8080";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // ──────────────────────────────────────────────
    //  Chat Sessions
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CreateSessionResponse> CreateSessionAsync(
        string accessToken, string userId, CancellationToken ct)
    {
        _logger.LogInformation("Creating chat session for user {UserId}", userId);

        var url = $"{_baseUrl}/api/chat/sessions";
        var body = new { userId };
        var request = CreateJsonRequest(HttpMethod.Post, url, accessToken, body);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "creating chat session", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<CreateSessionResponse>(content, _jsonOptions)
               ?? throw new InvalidOperationException("chatbot-service returned null session response");
    }

    /// <inheritdoc />
    public async Task<List<ChatSessionDto>> GetUserSessionsAsync(
        string accessToken, string userId, int skip, int limit, CancellationToken ct)
    {
        _logger.LogInformation("Fetching chat sessions for user {UserId}", userId);

        var url = $"{_baseUrl}/api/chat/sessions?skip={skip}&limit={limit}";
        var request = CreateRequest(HttpMethod.Get, url, accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "fetching chat sessions", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<ChatSessionDto>>(content, _jsonOptions)
               ?? new List<ChatSessionDto>();
    }

    /// <inheritdoc />
    public async Task<List<ChatMessageDto>> GetSessionMessagesAsync(
        string accessToken, string sessionId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching messages for session {SessionId}", sessionId);

        var url = $"{_baseUrl}/api/chat/sessions/{sessionId}/messages";
        var request = CreateRequest(HttpMethod.Get, url, accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "fetching session messages", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<ChatMessageDto>>(content, _jsonOptions)
               ?? new List<ChatMessageDto>();
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string accessToken, string sessionId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting chat session {SessionId}", sessionId);

        var url = $"{_baseUrl}/api/chat/sessions/{sessionId}";
        var request = CreateRequest(HttpMethod.Delete, url, accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "deleting chat session", ct);
    }

    // ──────────────────────────────────────────────
    //  Chat Streaming (SSE passthrough)
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<HttpResponseMessage> StreamMessageAsync(
        string accessToken, string sessionId, SendMessageRequest messageRequest, CancellationToken ct)
    {
        _logger.LogInformation("Starting SSE stream for session {SessionId}", sessionId);

        var url = $"{_baseUrl}/api/chat/stream/{sessionId}";
        var request = CreateJsonRequest(HttpMethod.Post, url, accessToken, messageRequest);

        // ResponseHeadersRead: don't buffer the full response — stream it as it arrives
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        await EnsureSuccessAsync(response, "streaming chat message", ct);

        return response;
    }

    // ──────────────────────────────────────────────
    //  RAG Synchronization
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task SyncKnowledgeAsync(
        string accessToken, SyncKnowledgeRequest syncRequest, CancellationToken ct)
    {
        _logger.LogInformation(
            "Syncing knowledge chunk: {SourceType}/{SourceId} ({Action})",
            syncRequest.SourceType, syncRequest.SourceId, syncRequest.Action);

        var url = $"{_baseUrl}/api/rag/sync";
        var request = CreateJsonRequest(HttpMethod.Post, url, accessToken, syncRequest);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, "syncing knowledge chunk", ct);
            _logger.LogInformation("Knowledge sync completed for {SourceType}/{SourceId}",
                syncRequest.SourceType, syncRequest.SourceId);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow — sync is a best-effort side-effect
            _logger.LogError(ex,
                "Failed to sync knowledge chunk {SourceType}/{SourceId}. " +
                "The chatbot's knowledge base may be stale until next reindex.",
                syncRequest.SourceType, syncRequest.SourceId);
        }
    }

    /// <inheritdoc />
    public async Task<ReindexKnowledgeResponse> ReindexKnowledgeAsync(
        string accessToken, CancellationToken ct)
    {
        _logger.LogInformation("Triggering full knowledge reindex on chatbot-service");

        var url = $"{_baseUrl}/api/rag/reindex";
        var request = CreateJsonRequest(HttpMethod.Post, url, accessToken, new { });

        var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "reindexing knowledge base", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<ReindexKnowledgeResponse>(content, _jsonOptions);

        _logger.LogInformation("Knowledge reindex completed: {TotalChunks} chunks indexed",
            result?.TotalChunksIndexed ?? 0);

        return result ?? new ReindexKnowledgeResponse { Success = false, TotalChunksIndexed = 0 };
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with JSON content and bearer token.
    /// </summary>
    private HttpRequestMessage CreateJsonRequest(
        HttpMethod method, string url, string accessToken, object body)
    {
        var request = CreateRequest(method, url, accessToken);
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with bearer token.
    /// </summary>
    private static HttpRequestMessage CreateRequest(
        HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    /// <summary>
    /// Reads the response and throws an <see cref="HttpRequestException"/> if the status code is not successful.
    /// </summary>
    private async Task EnsureSuccessAsync(
        HttpResponseMessage response, string context, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "chatbot-service returned {StatusCode} during {Context}: {Error}",
                (int)response.StatusCode, context, errorContent);
            response.EnsureSuccessStatusCode();
        }
    }
}
