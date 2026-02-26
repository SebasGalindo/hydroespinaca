'use client';

import { useMemo } from 'react';
import type { ChatSession } from '@hydroespinaca/shared';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface ChatSessionItemProps {
    session: ChatSession;
    isActive: boolean;
    onSelect: (id: string) => void;
    onDelete: (id: string) => void;
}

/**
 * Molecule — a single row in the session sidebar list.
 *
 * Shows the session title (truncated) and a relative timestamp.
 * A delete button appears on hover via Tailwind group utilities.
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

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onSelect(session.id);
        }
    };

    return (
        <div
            role="button"
            tabIndex={0}
            id={`chat-session-item-${session.id}`}
            className={[
                'group relative w-full flex items-start gap-1.5 px-3 py-2.5 rounded-lg',
                'text-left cursor-pointer transition-colors duration-150 border border-transparent outline-none focus-visible:ring-2 focus-visible:ring-green-500',
                isActive
                    ? 'bg-green-100/80 text-green-900 border-green-200 font-medium'
                    : 'hover:bg-white text-gray-700',
            ]
                .filter(Boolean)
                .join(' ')}
            onClick={() => onSelect(session.id)}
            onKeyDown={handleKeyDown}
            title={session.title}
            aria-current={isActive ? 'true' : undefined}
        >
            {/* Main content column */}
            <div className="flex-1 min-w-0">
                <span className="block truncate text-xs leading-snug">
                    {session.title || 'Nueva conversación'}
                </span>
                <span className={`block text-[10px] mt-0.5 ${isActive ? 'text-green-700' : 'text-gray-400'}`}>
                    {dateLabel}
                </span>
            </div>

            {/* Delete button — only visible on hover */}
            <button
                className={[
                    'flex-shrink-0 mt-0.5 p-1 rounded',
                    'opacity-0 group-hover:opacity-100 transition-opacity duration-150',
                    'hover:bg-red-100 text-gray-400 hover:text-red-500',
                ]
                    .filter(Boolean)
                    .join(' ')}
                onClick={handleDelete}
                aria-label={`Eliminar conversación: ${session.title}`}
                title="Eliminar"
                tabIndex={-1}
            >
                <XMarkIcon className="h-3.5 w-3.5" />
            </button>
        </div>
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
