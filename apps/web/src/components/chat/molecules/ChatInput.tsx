'use client';

import { useRef, useEffect } from 'react';
import { useChatStore } from '@hydroespinaca/shared';
import { PaperAirplaneIcon } from '@heroicons/react/24/solid';

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

    const isDisabled = isStreaming || !activeSessionId;

    return (
        <div className="flex items-end gap-2 px-3 py-3 border-t border-gray-200 bg-white">
            <textarea
                ref={textareaRef}
                id="chat-input-textarea"
                className={[
                    'flex-1 resize-none bg-gray-50 rounded-xl px-3 py-2 text-gray-800 text-sm border border-gray-200',
                    'placeholder:text-gray-400 focus:outline-none focus:ring-1 focus:ring-green-500 focus:border-green-500',
                    'min-h-[40px] leading-snug scrollbar-hidden',
                    isDisabled ? 'opacity-50 cursor-not-allowed bg-gray-100' : '',
                ]
                    .filter(Boolean)
                    .join(' ')}
                placeholder={
                    !activeSessionId
                        ? 'Selecciona o crea una conversación…'
                        : isStreaming
                            ? 'Esperando respuesta…'
                            : 'Escribe tu pregunta… (Enter para enviar)'
                }
                disabled={isDisabled}
                onKeyDown={handleKeyDown}
                rows={1}
                aria-label="Mensaje de chat"
            />
            <button
                id="chat-send-button"
                className={[
                    'flex-shrink-0 p-2.5 rounded-xl border border-transparent',
                    'bg-green-600 hover:bg-green-500',
                    'disabled:opacity-40 disabled:cursor-not-allowed',
                    'transition-colors duration-150',
                ]
                    .filter(Boolean)
                    .join(' ')}
                onClick={handleSend}
                disabled={isDisabled}
                aria-label="Enviar mensaje"
                title="Enviar"
            >
                <PaperAirplaneIcon className="h-5 w-5 text-white" />
            </button>
        </div>
    );
}
