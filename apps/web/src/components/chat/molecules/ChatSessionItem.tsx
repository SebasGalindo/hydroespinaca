'use client';

import { useMemo } from 'react';
import type { ChatSession } from '@hydroespinaca/shared';
import { XMarkIcon } from '@heroicons/react/24/outline';
import styles from './ChatSessionItem.module.css';

interface ChatSessionItemProps {
    session: ChatSession;
    isActive: boolean;
    onSelect: (id: string) => void;
    onDelete: (id: string) => void;
}

/**
 * Molecule — a single row in the session sidebar list.
 *
 * Shows the session title (truncated), last preview text, and a relative
 * timestamp. A delete button appears on hover via CSS.
 */
export function ChatSessionItem({
    session,
    isActive,
    onSelect,
    onDelete,
}: ChatSessionItemProps) {
    const dateLabel = useMemo(() => formatRelative(session.updatedAt), [session.updatedAt]);

    const handleDelete = (e: React.MouseEvent) => {
        e.stopPropagation();
        onDelete(session.id);
    };

    return (
        <button
            id={`chat-session-item-${session.id}`}
            className={[styles.item, isActive ? styles.active : ''].join(' ')}
            onClick={() => onSelect(session.id)}
            title={session.title}
            aria-current={isActive ? 'true' : undefined}
        >
            <span className={styles.title}>{session.title || 'Nueva conversación'}</span>
            <span className={styles.date}>{dateLabel}</span>

            <button
                className={styles.deleteBtn}
                onClick={handleDelete}
                aria-label={`Eliminar conversación: ${session.title}`}
                title="Eliminar"
                tabIndex={-1}
            >
                <XMarkIcon className={styles.deleteIcon} />
            </button>
        </button>
    );
}

// ──────────────────────────────────────
//  Helpers
// ──────────────────────────────────────

function formatRelative(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60_000);
    if (mins < 1) return 'Ahora';
    if (mins < 60) return `${mins} min`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs} h`;
    const days = Math.floor(hrs / 24);
    if (days < 7) return `${days} d`;
    if (days < 30) return `${Math.floor(days / 7)} sem`;
    return `${Math.floor(days / 30)} mes`;
}
