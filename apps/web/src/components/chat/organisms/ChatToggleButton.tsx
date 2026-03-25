'use client';
import React from 'react';

import { ChatBubbleOvalLeftEllipsisIcon, XMarkIcon } from '@heroicons/react/24/solid';

interface ChatToggleButtonProps {
    isOpen: boolean;
    onToggle: () => void;
}

/**
 * Organism — floating action button (FAB) fixed in the bottom-right corner.
 *
 * Toggles the `ChatDrawer` open/closed.
 * Shows an "AI" badge and pulses gently to invite interaction.
 */
export const ChatToggleButton = React.memo(function ChatToggleButton({ isOpen, onToggle }: ChatToggleButtonProps) {
    return (
        <button
            id="chat-toggle-fab"
            className={[
                'fixed bottom-6 right-6 z-[997]',
                'flex items-center justify-center gap-2',
                'px-4 h-12 rounded-2xl',
                'bg-gradient-to-br from-green-600 to-teal-700',
                'shadow-lg shadow-green-600/30 border border-green-500/50',
                'hover:shadow-xl hover:shadow-green-600/40 hover:scale-105',
                'transition-all duration-200',
                'text-white text-sm font-semibold',
                isOpen ? '' : 'animate-pulse [animation-iteration-count:3]',
            ]
                .filter(Boolean)
                .join(' ')}
            onClick={onToggle}
            aria-label={isOpen ? 'Cerrar chat' : 'Abrir asistente IA'}
            title={isOpen ? 'Cerrar chat' : 'Abrir asistente IA'}
        >
            {isOpen ? (
                <XMarkIcon className="h-5 w-5 flex-shrink-0" />
            ) : (
                <>
                    <ChatBubbleOvalLeftEllipsisIcon className="h-5 w-5 flex-shrink-0" />
                    <span className="text-xs font-bold tracking-wider">AI</span>
                </>
            )}
        </button>
    );
});
