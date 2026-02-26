'use client';

import { useMemo } from 'react';
import { useChatStore } from '@hydroespinaca/shared';
import { PlusIcon } from '@heroicons/react/24/outline';
import { ChatSessionItem } from '../molecules/ChatSessionItem';
import { SessionDateLabel } from '../atoms/SessionDateLabel';
import type { ChatSession } from '@hydroespinaca/shared';

/**
 * Organism — sidebar panel listing all sessions grouped by date.
 *
 * Groups sessions into temporal buckets (Hoy, Ayer, Esta semana …)
 * and renders a `SessionDateLabel` before each new group.
 * Contains a "Nueva conversación" button at the top.
 */
export function ChatSessionList() {
    const sessions = useChatStore((s) => s.sessions);
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const sessionsLoading = useChatStore((s) => s.sessionsLoading);
    const createSession = useChatStore((s) => s.createSession);
    const setActiveSession = useChatStore((s) => s.setActiveSession);
    const deleteSession = useChatStore((s) => s.deleteSession);
    const createSessionLoading = useChatStore((s) => s.createSessionLoading);

    // Group sessions by relative date label
    const grouped = useMemo(() => groupByDate(sessions), [sessions]);

    const handleNew = async () => {
        await createSession();
    };

    return (
        <aside
            id="chat-session-list"
            className="w-[220px] flex-shrink-0 flex flex-col border-r border-gray-200 bg-green-50/50 overflow-hidden"
        >
            {/* Header */}
            <div className="flex items-center justify-between px-3 py-3 border-b border-gray-200">
                <span className="text-[10px] font-bold uppercase tracking-widest text-green-800">
                    Conversaciones
                </span>
                <button
                    id="chat-new-session-button"
                    className="p-1.5 rounded-lg hover:bg-green-200 text-green-600 transition-colors duration-150 disabled:opacity-40"
                    onClick={handleNew}
                    disabled={createSessionLoading}
                    title="Nueva conversación"
                    aria-label="Nueva conversación"
                >
                    <PlusIcon className="h-4 w-4" />
                </button>
            </div>

            {/* Session list */}
            <div className="flex-1 overflow-y-auto py-1 px-1 space-y-0.5 scrollbar-thin">
                {sessionsLoading ? (
                    <p className="text-xs text-gray-500 px-3 py-4 text-center">Cargando…</p>
                ) : sessions.length === 0 ? (
                    <p className="text-xs text-gray-500 px-3 py-4 text-center">
                        Sin conversaciones previas.
                    </p>
                ) : (
                    grouped.map(({ label, items }) => (
                        <div key={label}>
                            <SessionDateLabel date={items[0]!.updatedAt} />
                            {items.map((s) => (
                                <ChatSessionItem
                                    key={s.id}
                                    session={s}
                                    isActive={s.id === activeSessionId}
                                    onSelect={setActiveSession}
                                    onDelete={deleteSession}
                                />
                            ))}
                        </div>
                    ))
                )}
            </div>
        </aside>
    );
}

// ──────────────────────────────────────
//  Helpers
// ──────────────────────────────────────

type Group = { label: string; items: ChatSession[] };

function groupByDate(sessions: ChatSession[]): Group[] {
    const groups = new Map<string, ChatSession[]>();
    for (const s of sessions) {
        const label = getLabel(s.updatedAt);
        if (!groups.has(label)) groups.set(label, []);
        groups.get(label)!.push(s);
    }
    return Array.from(groups.entries()).map(([label, items]) => ({ label, items }));
}

function getLabel(iso: string): string {
    const diff = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
    if (diff === 0) return 'Hoy';
    if (diff === 1) return 'Ayer';
    if (diff <= 6) return 'Esta semana';
    if (diff <= 29) return 'Este mes';
    return new Date(iso).getFullYear().toString();
}
