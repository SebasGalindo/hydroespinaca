// Chat domain types — shared between web and mobile

// ==================== Core Entities ====================

/**
 * Represents a chat conversation session belonging to a user.
 */
export interface ChatSession {
    /** UUID of the session */
    id: string;
    /** ID of the owner user */
    userId: string;
    /** Auto-generated title summarizing the first message */
    title: string;
    /** Whether the user has archived this session */
    isArchived: boolean;
    /** ISO 8601 creation timestamp */
    createdAt: string;
    /** ISO 8601 last-updated timestamp */
    updatedAt: string;
}

/**
 * Represents a single message within a chat session.
 */
export interface ChatMessage {
    /** Message author: user input, model response, or system instruction */
    role: 'user' | 'model' | 'system';
    /** Text content — Markdown formatted for model responses */
    content: string;
    /** ISO 8601 timestamp of when the message was created */
    timestamp: string;
    /** Number of tokens consumed by this message (for metrics, optional) */
    tokensUsed?: number;
}

// ==================== Requests ====================

/**
 * Optional context filters to narrow the RAG retrieval scope.
 */
export interface ChatContextFilters {
    /** Restrict vector search to a specific fuzzy system */
    fuzzySystemId?: string;
    /** How many hours back to query live sensor/actuator data (default: 24) */
    timeRangeHours?: number;
}

/**
 * Request body for sending a new user message in a chat session.
 */
export interface SendMessageRequest {
    /** The user's question or message */
    message: string;
    /** Optional filters to scope RAG context retrieval */
    contextFilters?: ChatContextFilters;
}

/**
 * Request body for creating a new chat session.
 * Currently empty — the session is initialized serverside.
 */
export interface CreateSessionRequest {
    /** Optional name hint; server may override with auto-generated title */
    titleHint?: string;
}

// ==================== Responses ====================

/**
 * Response returned after creating a new session.
 */
export interface CreateSessionResponse {
    sessionId: string;
}

// ==================== SSE Stream Events ====================

/**
 * SSE event payload for each streamed token fragment.
 * Corresponds to `event: token`.
 */
export interface StreamTokenEvent {
    /** One or more tokens emitted by the LLM */
    text: string;
}

/**
 * SSE event payload emitted when the stream finishes.
 * Corresponds to `event: done`.
 */
export interface StreamDoneEvent {
    /** Total tokens consumed in this request */
    tokensUsed: number;
    /** Auto-generated session title (may have been updated by the LLM) */
    sessionTitle: string;
}
