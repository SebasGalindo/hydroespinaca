'use client';

import { ChatSessionList } from './organisms/ChatSessionList';
import { MessageList } from './organisms/MessageList';
import { ChatInput } from './molecules/ChatInput';
import { useChatStream } from './hooks/useChatStream';
import { useChatSessions } from './hooks/useChatSessions';
import styles from './ChatDrawer.module.css';

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
 * Slides in from the right via CSS transform when `isOpen` is true.
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
                    className={styles.backdrop}
                    onClick={onClose}
                    aria-hidden="true"
                />
            )}

            {/* Drawer panel */}
            <div
                id="chat-drawer"
                className={[styles.drawer, isOpen ? styles.open : ''].join(' ')}
                role="dialog"
                aria-label="Asistente IA HydroEspinaca"
                aria-modal="true"
            >
                <ChatSessionList />

                <div className={styles.chatArea}>
                    <MessageList />
                    <ChatInput onSend={sendMessage} />
                </div>
            </div>
        </>
    );
}
