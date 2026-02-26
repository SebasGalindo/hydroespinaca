'use client';

import { ChatSessionList } from './organisms/ChatSessionList';
import { MessageList } from './organisms/MessageList';
import { ChatInput } from './molecules/ChatInput';
import { useChatStream } from './hooks/useChatStream';
import { useChatSessions } from './hooks/useChatSessions';

interface ChatDrawerProps {
    isOpen: boolean;
    onClose: () => void;
}

/**
 * Feature component — the full chat panel composed of organisms.
 *
 * Layout: two-column design.
 *   - Left  (220px): `ChatSessionList` — sidebar with session history.
 *   - Right (flex): `MessageList` (scrollable body) + `ChatInput` (sticky footer).
 *
 * Slides in from the right via Tailwind translate classes when `isOpen` is true.
 * The parent (layout) controls open/close state through `ChatToggleButton`.
 */
export function ChatDrawer({ isOpen, onClose }: ChatDrawerProps) {
    const { sendMessage } = useChatStream();

    // Load sessions when the drawer mounts
    useChatSessions();

    return (
        <>
            {/* Backdrop */}
            {isOpen && (
                <div
                    id="chat-drawer-backdrop"
                    className="fixed inset-0 bg-transparent z-[998]"
                    onClick={onClose}
                    aria-hidden="true"
                />
            )}

            {/* Drawer panel */}
            <div
                id="chat-drawer"
                className={[
                    'fixed inset-y-0 right-0 z-[999]',
                    'w-[min(680px,95vw)]',
                    'flex flex-row',
                    'bg-white border-l border-gray-200',
                    'shadow-[-8px_0_40px_rgba(0,0,0,0.1)]',
                    'transition-transform duration-300 ease-[cubic-bezier(0.4,0,0.2,1)]',
                    isOpen ? 'translate-x-0' : 'translate-x-full',
                ]
                    .filter(Boolean)
                    .join(' ')}
                role="dialog"
                aria-label="Asistente IA HydroEspinaca"
                aria-modal="true"
            >
                <ChatSessionList />

                <div className="flex-1 flex flex-col overflow-hidden min-w-0">
                    <MessageList />
                    <ChatInput onSend={sendMessage} />
                </div>
            </div>
        </>
    );
}
