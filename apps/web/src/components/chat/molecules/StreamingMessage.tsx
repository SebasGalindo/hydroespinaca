'use client';

import { useChatStore } from '@hydroespinaca/shared';
import { ChatBubbleWeb } from '../atoms/ChatBubbleWeb';
import { TypingCursor } from '../atoms/TypingCursor';

/**
 * Molecule — live bubble that accumulates SSE token fragments.
 *
 * Reads `streamingText` and `isStreaming` directly from the chat store
 * and renders the growing text with an animated `TypingCursor` suffix.
 * When streaming ends the finalised message will be rendered by
 * `MarkdownMessage` from the messages array — this component stays
 * mounted only while `isStreaming` is true.
 */
export function StreamingMessage() {
    const streamingText = useChatStore((s) => s.streamingText);
    const isStreaming = useChatStore((s) => s.isStreaming);

    if (!isStreaming && !streamingText) return null;

    return (
        <ChatBubbleWeb role="model" isStreaming={isStreaming}>
            <span className="whitespace-pre-wrap">{streamingText}</span>
            <TypingCursor visible={isStreaming} />
        </ChatBubbleWeb>
    );
}
