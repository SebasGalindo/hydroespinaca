'use client';

import { useEffect, useRef } from 'react';
import { useChatStore } from '@hydroespinaca/shared';
import { MarkdownMessage } from '../molecules/MarkdownMessage';
import { StreamingMessage } from '../molecules/StreamingMessage';
import styles from './MessageList.module.css';

/**
 * Organism — scrollable list of all messages in the active session.
 *
 * Renders the message history (completed messages as `MarkdownMessage`)
 * followed by the live stream bubble (`StreamingMessage`) if active.
 * Auto-scrolls to the bottom whenever messages or streamingText change.
 */
export function MessageList() {
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
            <div className={styles.empty}>
                <div className={styles.emptyIcon}>💬</div>
                <p className={styles.emptyTitle}>Asistente Inteligente HydroEspinaca</p>
                <p className={styles.emptySubtitle}>
                    Crea una nueva conversación o selecciona una existente para comenzar.
                </p>
            </div>
        );
    }

    if (messagesLoading) {
        return (
            <div className={styles.loading}>
                <div className={styles.spinner} />
            </div>
        );
    }

    return (
        <div id="chat-message-list" className={styles.list} role="log" aria-live="polite">
            {messages.length === 0 && !isStreaming ? (
                <div className={styles.empty}>
                    <p className={styles.emptySubtitle}>Escribe tu primera pregunta para comenzar.</p>
                </div>
            ) : (
                messages.map((msg, idx) => <MarkdownMessage key={idx} message={msg} />)
            )}
            <StreamingMessage />
            <div ref={bottomRef} />
        </div>
    );
}
