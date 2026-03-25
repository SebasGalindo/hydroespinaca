import { BaseApiService } from './BaseApiService';
import { getApiUrl, detectPlatform } from '../utils/apiConfig';
import { SessionStorage } from '../utils';
import type {
    ChatSession,
    ChatMessage,
    CreateSessionRequest,
    CreateSessionResponse,
    SendMessageRequest,
} from '../types/chat';

// ==================== Error ====================

/**
 * Typed error thrown by `ChatApiService` operations.
 * Re-uses the shared `ApiServiceError` convention.
 */
export class ChatApiError extends Error {
    constructor(
        public status: number,
        message: string,
        public code?: string
    ) {
        super(message);
        this.name = 'ChatApiError';
    }
}

// ==================== Service ====================

/**
 * API service for all chat-related endpoints exposed by the BFF.
 *
 * Endpoints proxied through the BFF:
 * - POST   /chat/sessions
 * - GET    /chat/sessions
 * - GET    /chat/messages/{sessionId}
 * - DELETE /chat/sessions/{sessionId}
 * - POST   /chat/stream/{sessionId}   ← SSE passthrough
 */
export class ChatApiService extends BaseApiService {
    constructor(baseUrl?: string) {
        super(baseUrl);
    }

    // ──────────────────────────────────────
    //  Sessions
    // ──────────────────────────────────────

    /**
     * Creates a new empty chat session for the current user.
     *
     * @param request Optional request with title hint.
     * @returns The newly created session's `sessionId`.
     */
    async createSession(request: CreateSessionRequest = {}): Promise<CreateSessionResponse> {
        return this.request<CreateSessionResponse>('/chat/sessions', {
            method: 'POST',
            body: request,
        });
    }

    /**
     * Lists all non-archived sessions for the current user,
     * ordered by `updatedAt` descending.
     *
     * @returns Array of `ChatSession` objects.
     */
    async getSessions(): Promise<ChatSession[]> {
        return (await this.request<ChatSession[]>('/chat/sessions')) ?? [];
    }

    /**
     * Retrieves the full message history for a specific session.
     *
     * @param sessionId UUID of the target session.
     * @returns Chronologically ordered array of `ChatMessage` objects.
     */
    async getMessages(sessionId: string): Promise<ChatMessage[]> {
        return (await this.request<ChatMessage[]>(`/chat/messages/${sessionId}`)) ?? [];
    }

    /**
     * Permanently deletes (or archives) a chat session.
     *
     * @param sessionId UUID of the session to remove.
     */
    async deleteSession(sessionId: string): Promise<void> {
        await this.deleteRequest(`/chat/sessions/${sessionId}`);
    }

    // ──────────────────────────────────────
    //  Streaming (SSE)
    // ──────────────────────────────────────

    /**
     * Sends a user message and returns the raw `Response` object
     * for SSE consumption. Callers must consume `response.body` as
     * a `ReadableStream` (web) or hand it to `react-native-sse` (mobile).
     *
     * The response is intentionally **not awaited to completion**:
     * set `HttpCompletionOption.ResponseHeadersRead` semantics.
     *
     * @param sessionId The target session UUID.
     * @param request   The user message + optional context filters.
     * @returns Raw `Response` whose `body` is the SSE stream.
     */
    async streamMessage(sessionId: string, request: SendMessageRequest): Promise<Response> {
        const baseUrl = getApiUrl();
        const url = `${baseUrl}/chat/stream/${sessionId}`;
        const platform = detectPlatform();

        const headers: Record<string, string> = {
            'Content-Type': 'application/json',
            Accept: 'text/event-stream',
        };

        // Mobile: attach session headers from secure storage
        if (platform === 'mobile') {
            const sessionId_ = await SessionStorage.getSessionId();
            const csrfToken = await SessionStorage.getCsrfToken();
            if (sessionId_) headers['X-Session-Id'] = sessionId_;
            if (csrfToken) headers['X-CSRF-Token'] = csrfToken;
        }

        const response = await fetch(url, {
            method: 'POST',
            headers,
            body: JSON.stringify(request),
            credentials: platform === 'mobile' ? 'omit' : 'include',
        });

        if (!response.ok) {
            let msg = `HTTP ${response.status}`;
            try {
                const err = await response.json();
                msg = String(err?.message ?? err?.detail ?? msg);
            } catch {
                // ignore parse errors
            }
            throw new ChatApiError(response.status, msg);
        }

        return response;
    }

    // ──────────────────────────────────────
    //  RAG Knowledge Management
    // ──────────────────────────────────────

    /**
     * Triggers a full re-indexation of all fuzzy knowledge chunks.
     * This rebuilds vector embeddings for all systems, variables, terms, and rules.
     * Only accessible to admin users.
     *
     * @returns Object with `success` and `totalChunksIndexed`.
     */
    async reindexKnowledge(): Promise<{ success: boolean; totalChunksIndexed: number }> {
        return this.request<{ success: boolean; totalChunksIndexed: number }>(
            '/chat/reindex-knowledge',
            { method: 'POST' }
        );
    }
}

// ==================== Singleton ====================

/** Pre-instantiated singleton — import this in stores and hooks. */
export const chatService = new ChatApiService();
