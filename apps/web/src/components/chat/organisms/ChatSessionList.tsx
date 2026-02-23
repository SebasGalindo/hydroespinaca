'use client';

import { useMemo } from 'react';
import { useChatStore } from '@hydroespinaca/shared';
import { PlusIcon } from '@heroicons/react/24/outline';
import { ChatSessionItem } from '../molecules/ChatSessionItem';
import { SessionDateLabel } from '../atoms/SessionDateLabel';
import styles from './ChatSessionList.module.css';

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
        <aside id="chat-session-list" className={styles.panel}>
            <div className={styles.header}>
                <span className={styles.headerTitle}>Conversaciones</span>
                <button
                    id="chat-new-session-button"
                    className={styles.newBtn}
                    onClick={handleNew}
                    disabled={createSessionLoading}
                    title="Nueva conversación"
                    aria-label="Nueva conversación"
                >
                    <PlusIcon className={styles.newIcon} />
                </button>
            </div>

            <div className={styles.list}>
                {sessionsLoading ? (
                    <p className={styles.loadingText}>Cargando…</p>
                ) : sessions.length === 0 ? (
                    <p className={styles.emptyText}>Sin conversaciones previas.</p>
                ) : (
                    grouped.map(({ label, items }) => (
                        <div key={label}>
                            <SessionDateLabel date={items[0].updatedAt} />
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

type Group = { label: string; items: import('@hydroespinaca/shared').ChatSession[] };

function groupByDate(sessions: import('@hydroespinaca/shared').ChatSession[]): Group[] {
    const groups = new Map<string, import('@hydroespinaca/shared').ChatSession[]>();
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
