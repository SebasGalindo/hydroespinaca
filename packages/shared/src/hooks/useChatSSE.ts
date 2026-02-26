import { useCallback, useRef } from 'react';
import { useChatStore } from '../store/chatStore';
import { getApiUrl, detectPlatform } from '../utils/apiConfig';
import { SessionStorage } from '../utils/secureStorage.native';
import type { SendMessageRequest, StreamTokenEvent, StreamDoneEvent } from '../types/chat';

// ── EventSource type shim ────────────────────────────────────────────────────
// React Native ≥ 0.73 / Expo SDK ≥ 50 ships EventSource globally on the JS
// runtime.  If it is not present at runtime we log a clear error and bail.
// We do NOT polyfill here – the consuming app must ensure EventSource is
// available (either native or via `react-native-sse`).
// ─────────────────────────────────────────────────────────────────────────────

type NativeEventSource = {
    close(): void;
    addEventListener(type: string, listener: (event: { data: string }) => void): void;
    removeEventListener(type: string, listener: (event: { data: string }) => void): void;
};

/**
 * Build the fetch URL and auth headers for the /chat/stream endpoint.
 * On mobile, auth is header-based (X-Session-Id / X-CSRF-Token).
 */
async function buildStreamHeaders(sessionId: string): Promise<Record<string, string>> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
    };

    const [sessionToken, csrfToken] = await Promise.all([
        SessionStorage.getSessionId(),
        SessionStorage.getCsrfToken(),
    ]);

    if (sessionToken) headers['X-Session-Id'] = sessionToken;
    if (csrfToken) headers['X-CSRF-Token'] = csrfToken;

    return headers;
}

/**
 * Hook — manages Server-Sent Events streaming for the **mobile** chat.
 *
 * Mirrors `useChatStream` (web) but uses the global `EventSource` instead of
 * `ReadableStream` because React Native does not expose `response.body` as a
 * WHATWG stream.
 *
 * Requirements:
 *  - React Native ≥ 0.73 **or** Expo SDK ≥ 50 (EventSource is global).
 *  - If EventSource is unavailable, install `react-native-sse` and assign its
 *    class to `global.EventSource` in your app entry point.
 *
 * Usage:
 * ```tsx
 * const { sendMessage, isStreaming } = useChatSSE();
 * ```
 */
export function useChatSSE() {
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const startStream = useChatStore((s) => s.startStream);
    const appendStreamToken = useChatStore((s) => s.appendStreamToken);
    const finalizeStream = useChatStore((s) => s.finalizeStream);
    const isStreaming = useChatStore((s) => s.isStreaming);

    // Keep a ref to the current ES instance so we can close it on unmount /
    // if a new message arrives before the previous stream finishes.
    const esRef = useRef<NativeEventSource | null>(null);

    const sendMessage = useCallback(
        async (userMessage: string) => {
            if (!activeSessionId || isStreaming) return;

            // Verify EventSource is available in this environment
            if (typeof (globalThis as Record<string, unknown>).EventSource === 'undefined') {
                console.error(
                    '[useChatSSE] EventSource is not available in this environment.\n' +
                    'Make sure you are using React Native ≥ 0.73 / Expo SDK ≥ 50, or\n' +
                    'install react-native-sse and polyfill global.EventSource.'
                );
                return;
            }

            // Close any previous stream that may still be open
            esRef.current?.close();
            esRef.current = null;

            const platform = detectPlatform();
            if (platform !== 'mobile') {
                console.warn('[useChatSSE] This hook is intended for mobile only. Use useChatStream on web.');
            }

            const baseUrl = getApiUrl();
            const url = `${baseUrl}/chat/stream/${activeSessionId}`;

            let headers: Record<string, string>;
            try {
                headers = await buildStreamHeaders(activeSessionId);
            } catch (err) {
                console.error('[useChatSSE] Failed to build auth headers:', err);
                return;
            }

            const body: SendMessageRequest = { message: userMessage };
            startStream();

            try {
                // Some EventSource polyfills (react-native-sse) accept an options object
                // with method/body/headers. The native Expo EventSource also supports POST.
                const EventSourceCtor = (globalThis as Record<string, unknown>).EventSource as {
                    new(
                        url: string,
                        init?: {
                            method?: string;
                            body?: string;
                            headers?: Record<string, string>;
                        }
                    ): NativeEventSource;
                };

                const es = new EventSourceCtor(url, {
                    method: 'POST',
                    body: JSON.stringify(body),
                    headers,
                });

                esRef.current = es;

                // ── SSE event handlers ────────────────────────────────────────
                const handleToken = (event: { data: string }) => {
                    try {
                        const payload = JSON.parse(event.data) as StreamTokenEvent;
                        appendStreamToken(payload.text);
                    } catch {
                        // Malformed token event — skip silently
                    }
                };

                const handleDone = (event: { data: string }) => {
                    try {
                        const payload = JSON.parse(event.data) as StreamDoneEvent;
                        finalizeStream(payload);
                    } catch {
                        finalizeStream({ tokensUsed: 0, sessionTitle: '' });
                    } finally {
                        es.close();
                        esRef.current = null;
                    }
                };

                const handleError = () => {
                    finalizeStream({ tokensUsed: 0, sessionTitle: '' });
                    es.close();
                    esRef.current = null;
                    console.error('[useChatSSE] Stream error received from server.');
                };

                es.addEventListener('token', handleToken);
                es.addEventListener('done', handleDone);
                es.addEventListener('error', handleError);
            } catch (error) {
                finalizeStream({ tokensUsed: 0, sessionTitle: '' });
                console.error('[useChatSSE] Failed to open stream:', error);
            }
        },
        [activeSessionId, isStreaming, startStream, appendStreamToken, finalizeStream]
    );

    return { sendMessage, isStreaming };
}
