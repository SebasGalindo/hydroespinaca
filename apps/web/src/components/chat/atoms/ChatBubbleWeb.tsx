import React from 'react';

interface ChatBubbleWebProps {
    /** Message author — controls visual alignment and colour */
    role: 'user' | 'model' | 'system';
    /** Plain text content (use MarkdownMessage molecule for Markdown) */
    children: React.ReactNode;
    /** When true renders a pulsing left-border indicator */
    isStreaming?: boolean;
    /** Optional additional class names */
    className?: string;
}

/**
 * Atom — base visual container for a single chat message.
 *
 * User bubbles are right-aligned with the primary brand colour.
 * Bot/model bubbles are left-aligned with a neutral dark surface.
 * System messages are centred in muted italic text.
 *
 * Contains NO business logic — use molecules/organisms above it.
 */
export const ChatBubbleWeb = React.memo(function ChatBubbleWeb({
    role,
    children,
    isStreaming = false,
    className = '',
}: ChatBubbleWebProps) {
    const wrapperClass = [
        'flex w-full py-1',
        role === 'user' ? 'justify-end' : role === 'system' ? 'justify-center' : 'justify-start',
        className,
    ]
        .filter(Boolean)
        .join(' ');

    const bubbleClass = [
        'max-w-[78%] px-3.5 py-2.5 rounded-2xl text-sm leading-relaxed break-words',
        role === 'user'
            ? 'bg-gradient-to-br from-green-600 to-green-700 text-white rounded-br-sm shadow-sm'
            : role === 'system'
                ? 'bg-transparent italic text-xs text-gray-500 text-center max-w-full'
                : [
                    'bg-gray-50 border border-gray-200 text-gray-800 rounded-bl-sm shadow-sm',
                    isStreaming ? 'border-l-2 border-l-green-500' : '',
                ]
                    .filter(Boolean)
                    .join(' '),
    ]
        .filter(Boolean)
        .join(' ');

    return (
        <div className={wrapperClass} data-role={role}>
            <div className={bubbleClass}>{children}</div>
        </div>
    );
});
