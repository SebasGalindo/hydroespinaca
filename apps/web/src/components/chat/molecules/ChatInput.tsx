'use client';

import { useRef, useEffect } from 'react';
import { useChatStore } from '@hydroespinaca/shared';
import { PaperAirplaneIcon } from '@heroicons/react/24/solid';
import styles from './ChatInput.module.css';

interface ChatInputProps {
    onSend: (message: string) => void;
}

/**
 * Molecule — auto-expanding textarea for composing user messages.
 *
 * - Enter sends the message; Shift+Enter inserts a newline.
 * - Auto-grows up to 5 lines then scrolls.
 * - Disabled while `isStreaming` is true in the store.
 * - Send button icon reflects disabled state.
 */
export function ChatInput({ onSend }: ChatInputProps) {
    const isStreaming = useChatStore((s) => s.isStreaming);
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    // Auto-resize the textarea as content grows
    useEffect(() => {
        const ta = textareaRef.current;
        if (!ta) return;
        ta.style.height = 'auto';
        const lineHeight = parseInt(getComputedStyle(ta).lineHeight || '20', 10);
        const maxH = lineHeight * 5 + 24; // 5 lines + padding
        ta.style.height = Math.min(ta.scrollHeight, maxH) + 'px';
    });

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    const handleSend = () => {
        const ta = textareaRef.current;
        if (!ta) return;
        const text = ta.value.trim();
        if (!text || isStreaming || !activeSessionId) return;
        onSend(text);
        ta.value = '';
        ta.style.height = 'auto';
    };

    return (
        <div className={styles.wrapper}>
            <textarea
                ref={textareaRef}
                id="chat-input-textarea"
                className={styles.textarea}
                placeholder={
                    !activeSessionId
                        ? 'Selecciona o crea una conversación…'
                        : isStreaming
                            ? 'Esperando respuesta…'
                            : 'Escribe tu pregunta… (Enter para enviar)'
                }
                disabled={isStreaming || !activeSessionId}
                onKeyDown={handleKeyDown}
                rows={1}
                aria-label="Mensaje de chat"
            />
            <button
                id="chat-send-button"
                className={styles.sendBtn}
                onClick={handleSend}
                disabled={isStreaming || !activeSessionId}
                aria-label="Enviar mensaje"
                title="Enviar"
            >
                <PaperAirplaneIcon className={styles.sendIcon} />
            </button>
        </div>
    );
}
