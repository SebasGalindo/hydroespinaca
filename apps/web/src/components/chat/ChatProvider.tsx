'use client';

import { useState } from 'react';
import { useAuthStore } from '@hydroespinaca/shared';
import { ChatDrawer } from '@/components/chat/ChatDrawer';
import { ChatToggleButton } from '@/components/chat/organisms/ChatToggleButton';

/**
 * Client component that wraps the ChatDrawer and ChatToggleButton.
 * Mounted globally in the root layout so the chat is accessible from
 * every page. Only renders when the user is authenticated.
 */
export function ChatProvider() {
    const [isOpen, setIsOpen] = useState(false);
    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

    if (!isAuthenticated) return null;

    return (
        <>
            <ChatToggleButton isOpen={isOpen} onToggle={() => setIsOpen((o) => !o)} />
            <ChatDrawer isOpen={isOpen} onClose={() => setIsOpen(false)} />
        </>
    );
}
