'use client';

import { useCallback } from 'react';
import { useChatStore, chatService } from '@hydroespinaca/shared';
import type { StreamTokenEvent, StreamDoneEvent, SendMessageRequest } from '@hydroespinaca/shared';

/**
 * Hook — manages Server-Sent Events streaming for the web chat.
 *
 * Reads `activeSessionId` from the store, calls `chatService.streamMessage`,
 * and channels each token into `appendStreamToken` / `finalizeStream` on the
 * shared `useChatStore`.
 *
 * The hook also optimistically appends the user message to `messages` before
 * the stream starts, so the UI feels instant.
 */
export function useChatStream() {
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const startStream = useChatStore((s) => s.startStream);
    const appendStreamToken = useChatStore((s) => s.appendStreamToken);
    const finalizeStream = useChatStore((s) => s.finalizeStream);
    const appendUserMessage = useChatStore((s) => s.appendUserMessage);
    const isStreaming = useChatStore((s) => s.isStreaming);

    const sendMessage = useCallback(
        async (userMessage: string) => {
            if (!activeSessionId || isStreaming) return;

            const request: SendMessageRequest = { message: userMessage };

            // Optimistically update UI
            appendUserMessage(userMessage);
            startStream();

            try {
                const response = await chatService.streamMessage(activeSessionId, request);
                const reader = response.body!.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                while (true) {
                    const { done, value } = await reader.read();
                    if (done) break;

                    buffer += decoder.decode(value, { stream: true });

                    // Process complete SSE lines
                    const lines = buffer.split('\n');
                    buffer = lines.pop() ?? ''; // keep the incomplete last chunk

                    let currentEvent = '';
                    for (const line of lines) {
                        if (line.startsWith('event: ')) {
                            currentEvent = line.slice(7).trim();
                        } else if (line.startsWith('data: ')) {
                            const rawData = line.slice(6).trim();
                            if (!rawData) continue;
                            try {
                                const payload = JSON.parse(rawData);
                                if (currentEvent === 'token' || 'text' in payload) {
                                    appendStreamToken((payload as StreamTokenEvent).text);
                                } else if (currentEvent === 'done' || 'tokensUsed' in payload) {
                                    finalizeStream(payload as StreamDoneEvent);
                                }
                            } catch {
                                // Malformed JSON — skip
                            }
                        }
                    }
                }
            } catch (error) {
                // Stream failed — finalize with error message so UI recovers gracefully
                finalizeStream({ tokensUsed: 0, sessionTitle: '' });
                console.error('[useChatStream] Stream error:', error);
            }
        },
        [activeSessionId, isStreaming, startStream, appendStreamToken, finalizeStream]
    );

    return { sendMessage, isStreaming };
}
