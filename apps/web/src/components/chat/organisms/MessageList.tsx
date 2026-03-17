'use client';

import React, { useEffect, useRef } from 'react';
import dynamic from 'next/dynamic';
import { useChatStore } from '@hydroespinaca/shared';
import { StreamingMessage } from '../molecules/StreamingMessage';

const MarkdownMessage = dynamic(
  () => import('../molecules/MarkdownMessage').then(m => ({ default: m.MarkdownMessage })),
  { ssr: false, loading: () => <div className="animate-pulse h-6 bg-gray-200 rounded my-1" /> }
);

/**
 * Organism — scrollable list of all messages in the active session.
 *
 * Renders the message history (completed messages as `MarkdownMessage`)
 * followed by the live stream bubble (`StreamingMessage`) if active.
 * Auto-scrolls to the bottom whenever messages or streamingText change.
 */
export const MessageList = React.memo(function MessageList() {
    const messages = useChatStore((s) => s.messages);
    const isStreaming = useChatStore((s) => s.isStreaming);
    const messagesLoading = useChatStore((s) => s.messagesLoading);
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const bottomRef = useRef<HTMLDivElement>(null);

    // Auto-scroll to bottom on new content
    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages.length, isStreaming]);

    if (!activeSessionId) {
        return (
            <div className="flex flex-col items-center justify-center flex-1 gap-3 p-8 text-center">
                <span className="text-4xl select-none" aria-hidden="true">
                    💬
                </span>
                <p className="text-sm font-bold text-green-800">
                    Asistente Inteligente HydroEspinaca
                </p>
                <p className="text-xs text-gray-500 max-w-xs leading-relaxed">
                    Crea una nueva conversación o selecciona una existente para comenzar.
                </p>
            </div>
        );
    }

    if (messagesLoading) {
        return (
            <div className="flex items-center justify-center flex-1">
                <div className="h-8 w-8 rounded-full border-2 border-gray-200 border-t-green-500 animate-spin" />
            </div>
        );
    }

    return (
        <div
            id="chat-message-list"
            className="flex-1 overflow-y-auto px-4 py-3 space-y-0.5 scrollbar-thin"
            role="log"
            aria-live="polite"
        >
            {messages.length === 0 && !isStreaming ? (
                <div className="flex items-center justify-center h-full">
                    <p className="text-xs text-gray-500">Escribe tu primera pregunta para comenzar.</p>
                </div>
            ) : (
                messages.map((msg, idx) => <MarkdownMessage key={idx} message={msg} />)
            )}
            <StreamingMessage />
            <div ref={bottomRef} />
        </div>
    );
});
