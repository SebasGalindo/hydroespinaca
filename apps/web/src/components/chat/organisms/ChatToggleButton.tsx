'use client';

import { ChatBubbleOvalLeftEllipsisIcon } from '@heroicons/react/24/solid';
import styles from './ChatToggleButton.module.css';

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
export function ChatToggleButton({ isOpen, onToggle }: ChatToggleButtonProps) {
    return (
        <button
            id="chat-toggle-fab"
            className={[styles.fab, isOpen ? styles.open : ''].join(' ')}
            onClick={onToggle}
            aria-label={isOpen ? 'Cerrar chat' : 'Abrir asistente IA'}
            title={isOpen ? 'Cerrar chat' : 'Abrir asistente IA'}
        >
            <ChatBubbleOvalLeftEllipsisIcon className={styles.icon} />
            <span className={styles.badge}>AI</span>
        </button>
    );
}
