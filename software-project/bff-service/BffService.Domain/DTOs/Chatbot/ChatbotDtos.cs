namespace BffService.Domain.DTOs.Chatbot;

/// <summary>
/// Response returned when a new chat session is created.
/// </summary>
public class CreateSessionResponse
{
    /// <summary>
    /// Unique identifier of the created session.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// A single chat session summary (for listing).
/// </summary>
public class ChatSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// A single chat message within a session.
/// </summary>
public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? TokensUsed { get; set; }
}

/// <summary>
/// Request body for the send-message (stream) endpoint.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// The user's message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional context filters for the RAG retrieval.
    /// </summary>
    public ContextFiltersDto? ContextFilters { get; set; }
}

/// <summary>
/// Optional filters to narrow down the RAG context.
/// </summary>
public class ContextFiltersDto
{
    public string? FuzzySystemId { get; set; }
    public int? TimeRangeHours { get; set; }
}

/// <summary>
/// Request to synchronize a knowledge chunk (re-vectorize on fuzzy entity change).
/// </summary>
public class SyncKnowledgeRequest
{
    /// <summary>
    /// Type of source entity: fuzzy_rule, fuzzy_system, fuzzy_variable.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the source entity in the original service.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Action to perform: "upsert" or "delete".
    /// </summary>
    public string Action { get; set; } = "upsert";
}
