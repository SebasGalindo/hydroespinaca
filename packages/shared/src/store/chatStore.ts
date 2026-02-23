import { create } from 'zustand';
import { chatService, ChatApiError } from '../api/chatService';
import type {
    ChatSession,
    ChatMessage,
    StreamDoneEvent,
    SendMessageRequest,
} from '../types/chat';

// ==================== State Interface ====================

interface ChatState {
    /** Ordered list of sessions (most recently updated first) */
    sessions: ChatSession[];
    sessionsLoading: boolean;
    sessionsError: string | null;

    /** UUID of the currently active session */
    activeSessionId: string | null;

    /** Messages for the active session */
    messages: ChatMessage[];
    messagesLoading: boolean;
    messagesError: string | null;

    /**
     * Text accumulated from SSE token events during an active stream.
     * Cleared when the stream finalizes or a new session becomes active.
     */
    streamingText: string;
    /** True while an SSE stream is in progress */
    isStreaming: boolean;

    /** Loading flag for session creation */
    createSessionLoading: boolean;
    createSessionError: string | null;

    /** Loading flag for session deletion */
    deleteSessionLoading: boolean;
    deleteSessionError: string | null;
}

// ==================== Actions Interface ====================

interface ChatActions {
    // Sessions
    /** Loads all sessions for the authenticated user. */
    fetchSessions: () => Promise<void>;
    /**
     * Creates a new empty session and sets it as active.
     * @returns The newly created `sessionId`.
     */
    createSession: () => Promise<string>;
    /** Deletes a session and removes it from the list. */
    deleteSession: (sessionId: string) => Promise<void>;
    /** Sets the active session and re-fetches its messages. */
    setActiveSession: (sessionId: string) => void;

    // Messages
    /**
     * Loads the message history for the given session.
     * Called internally by `setActiveSession`.
     */
    fetchMessages: (sessionId: string) => Promise<void>;

    // Streaming
    /**
     * Appends a token fragment to `streamingText`.
     * Called by `useChatStream` (web) or `useChatSSE` (mobile).
     */
    appendStreamToken: (token: string) => void;
    /**
     * Finalizes an active SSE stream: moves `streamingText` into the
     * messages array as a completed model message and resets streaming state.
     */
    finalizeStream: (event: StreamDoneEvent) => void;
    /** Marks streaming as started before the first token arrives. */
    startStream: () => void;

    // Utility
    /** Clears all error fields. */
    clearErrors: () => void;
    /** Resets the store to initial state (e.g. on logout). */
    resetChatStore: () => void;
}

// ==================== Initial State ====================

const initialState: ChatState = {
    sessions: [],
    sessionsLoading: false,
    sessionsError: null,

    activeSessionId: null,

    messages: [],
    messagesLoading: false,
    messagesError: null,

    streamingText: '',
    isStreaming: false,

    createSessionLoading: false,
    createSessionError: null,

    deleteSessionLoading: false,
    deleteSessionError: null,
};

// ==================== Helpers ====================

const extractErrorMessage = (error: unknown): string => {
    if (error instanceof ChatApiError) {
        if (error.status === 401) return 'Sesión expirada. Por favor, inicia sesión nuevamente.';
        if (error.status === 403) return 'No tienes permisos para acceder al chat.';
        if (error.status === 404) return 'Sesión de chat no encontrada.';
        return error.message;
    }
    if (error instanceof Error) return error.message;
    return 'Error desconocido';
};

// ==================== Store ====================

export const useChatStore = create<ChatState & ChatActions>()((set, get) => ({
    ...initialState,

    // ────────────────────────────────────────
    //  Sessions
    // ────────────────────────────────────────

    fetchSessions: async () => {
        set({ sessionsLoading: true, sessionsError: null });
        try {
            const sessions = await chatService.getSessions();
            set({ sessions, sessionsLoading: false });
        } catch (error) {
            set({ sessionsError: extractErrorMessage(error), sessionsLoading: false });
        }
    },

    createSession: async () => {
        set({ createSessionLoading: true, createSessionError: null });
        try {
            const { sessionId } = await chatService.createSession();
            // Refetch session list to get the full session object
            const sessions = await chatService.getSessions();
            set({
                sessions,
                activeSessionId: sessionId,
                messages: [],
                streamingText: '',
                isStreaming: false,
                createSessionLoading: false,
            });
            return sessionId;
        } catch (error) {
            set({
                createSessionError: extractErrorMessage(error),
                createSessionLoading: false,
            });
            throw error;
        }
    },

    deleteSession: async (sessionId: string) => {
        set({ deleteSessionLoading: true, deleteSessionError: null });
        try {
            await chatService.deleteSession(sessionId);
            set((state) => ({
                sessions: state.sessions.filter((s) => s.id !== sessionId),
                activeSessionId: state.activeSessionId === sessionId ? null : state.activeSessionId,
                messages: state.activeSessionId === sessionId ? [] : state.messages,
                deleteSessionLoading: false,
            }));
        } catch (error) {
            set({
                deleteSessionError: extractErrorMessage(error),
                deleteSessionLoading: false,
            });
            throw error;
        }
    },

    setActiveSession: (sessionId: string) => {
        if (get().activeSessionId === sessionId) return;
        set({ activeSessionId: sessionId, messages: [], streamingText: '', isStreaming: false });
        get().fetchMessages(sessionId);
    },

    // ────────────────────────────────────────
    //  Messages
    // ────────────────────────────────────────

    fetchMessages: async (sessionId: string) => {
        set({ messagesLoading: true, messagesError: null });
        try {
            const messages = await chatService.getMessages(sessionId);
            set({ messages, messagesLoading: false });
        } catch (error) {
            set({ messagesError: extractErrorMessage(error), messagesLoading: false });
        }
    },

    // ────────────────────────────────────────
    //  Streaming
    // ────────────────────────────────────────

    startStream: () => {
        // Optimistically append the user message placeholder is handled by the hook caller.
        // Here we only reset streaming text and mark stream as active.
        set({ isStreaming: true, streamingText: '' });
    },

    appendStreamToken: (token: string) => {
        set((state) => ({ streamingText: state.streamingText + token }));
    },

    finalizeStream: (event: StreamDoneEvent) => {
        set((state) => {
            const completedMessage: ChatMessage = {
                role: 'model',
                content: state.streamingText,
                timestamp: new Date().toISOString(),
            };
            // Update session title if changed by LLM
            const sessions = state.sessions.map((s) =>
                s.id === state.activeSessionId
                    ? { ...s, title: event.sessionTitle, updatedAt: new Date().toISOString() }
                    : s
            );
            return {
                messages: [...state.messages, completedMessage],
                streamingText: '',
                isStreaming: false,
                sessions,
            };
        });
    },

    // ────────────────────────────────────────
    //  Utility
    // ────────────────────────────────────────

    clearErrors: () => {
        set({
            sessionsError: null,
            messagesError: null,
            createSessionError: null,
            deleteSessionError: null,
        });
    },

    resetChatStore: () => {
        set(initialState);
    },
}));
