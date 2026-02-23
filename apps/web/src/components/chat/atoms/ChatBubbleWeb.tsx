import React from 'react';
import styles from './ChatBubbleWeb.module.css';

interface ChatBubbleWebProps {
    /** Message author — controls visual alignment and colour */
    role: 'user' | 'model' | 'system';
    /** Plain text content (use MarkdownMessage molecule for Markdown) */
    children: React.ReactNode;
    /** When true renders a pulsing skeleton shimmer */
    isStreaming?: boolean;
    /** Optional additional class names */
    className?: string;
}

/**
 * Atom — base visual container for a single chat message.
 *
 * User bubbles are right-aligned with the primary brand colour.
 * Bot/model bubbles are left-aligned with a neutral surface.
 * System messages are centred in muted italic text.
 *
 * Contains NO business logic — use molecules/organisms above it.
 */
export function ChatBubbleWeb({
    role,
    children,
    isStreaming = false,
    className = '',
}: ChatBubbleWebProps) {
    return (
        <div
            className={[
                styles.wrapper,
                styles[role],
                isStreaming ? styles.streaming : '',
                className,
            ]
                .filter(Boolean)
                .join(' ')}
            data-role={role}
        >
            <div className={styles.bubble}>{children}</div>
        </div>
    );
}
