'use client';

import { useEffect } from 'react';
import { useChatStore } from '@hydroespinaca/shared';

/**
 * Hook — fetches sessions on first mount and exposes session store state.
 *
 * Thin wrapper that triggers the initial session load and keeps the
 * component tree clean by hiding store subscription details.
 */
export function useChatSessions() {
    const sessions = useChatStore((s) => s.sessions);
    const fetchSessions = useChatStore((s) => s.fetchSessions);
    const sessionsLoading = useChatStore((s) => s.sessionsLoading);

    useEffect(() => {
        fetchSessions();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return { sessions, sessionsLoading, fetchSessions };
}
