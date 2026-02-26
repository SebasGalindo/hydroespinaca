import React, { useEffect, useRef } from 'react';
import { FlatList, View, Text, ActivityIndicator, StyleSheet } from 'react-native';
import { useChatStore, colors, spacing } from '@hydroespinaca/shared';
import type { ChatMessage } from '@hydroespinaca/shared';
import { ChatMessageItem } from '../molecules/ChatMessageItem';
import { ChatBubbleMobile } from '../atoms/ChatBubbleMobile';
import { TypingIndicator } from '../atoms/TypingIndicator';

/**
 * Organism — scrollable list of all messages in the active session.
 *
 * Uses FlatList for performance (virtualised). Renders the message history
 * followed by a streaming partial message and/or typing indicator.
 * Auto-scrolls to the latest content whenever messages or streamingText change.
 */
export function ChatMessageList(): React.ReactElement {
    const messages = useChatStore((s) => s.messages);
    const isStreaming = useChatStore((s) => s.isStreaming);
    const streamingText = useChatStore((s) => s.streamingText);
    const messagesLoading = useChatStore((s) => s.messagesLoading);
    const activeSessionId = useChatStore((s) => s.activeSessionId);

    const listRef = useRef<FlatList<ChatMessage>>(null);

    // Auto-scroll when new content arrives
    useEffect(() => {
        if (messages.length > 0 || isStreaming) {
            // Small delay ensures the list has re-rendered before scrolling
            const timer = setTimeout(() => {
                listRef.current?.scrollToEnd({ animated: true });
            }, 100);
            return () => clearTimeout(timer);
        }
        return undefined;
    }, [messages.length, streamingText, isStreaming]);

    // ── Empty state (no active session) ─────────────────────────────────────
    if (!activeSessionId) {
        return (
            <View style={styles.empty}>
                <Text style={styles.emptyIcon}>💬</Text>
                <Text style={styles.emptyTitle}>Asistente Inteligente</Text>
                <Text style={styles.emptySubtitle}>
                    Selecciona o crea una conversación para comenzar.
                </Text>
            </View>
        );
    }

    // ── Loading state ───────────────────────────────────────────────────────
    if (messagesLoading) {
        return (
            <View style={styles.loading}>
                <ActivityIndicator size="large" color={colors.hidro[600]} />
            </View>
        );
    }

    // ── Footer: SSE streaming partial + typing indicator ────────────────────
    const renderFooter = () => {
        if (!isStreaming && !streamingText) return null;

        return (
            <View>
                {streamingText ? (
                    <ChatBubbleMobile role="model" isStreaming={isStreaming}>
                        <Text style={styles.streamingText}>{streamingText}</Text>
                    </ChatBubbleMobile>
                ) : null}
                <View style={styles.typingContainer}>
                    <TypingIndicator visible={isStreaming && !streamingText} />
                </View>
            </View>
        );
    };

    // ── Empty messages state ────────────────────────────────────────────────
    const renderEmpty = () => {
        if (isStreaming) return null;
        return (
            <View style={styles.emptyMessages}>
                <Text style={styles.emptySubtitle}>
                    Escribe tu primera pregunta para comenzar.
                </Text>
            </View>
        );
    };

    return (
        <FlatList<ChatMessage>
            ref={listRef}
            data={messages}
            keyExtractor={(_, index) => index.toString()}
            renderItem={({ item }) => <ChatMessageItem message={item} />}
            style={styles.container}
            contentContainerStyle={[
                styles.content,
                messages.length === 0 && !isStreaming ? styles.contentEmpty : undefined,
            ]}
            ListFooterComponent={renderFooter}
            ListEmptyComponent={renderEmpty}
            showsVerticalScrollIndicator={false}
            keyboardDismissMode="interactive"
            keyboardShouldPersistTaps="handled"
        />
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: colors.gray[50],
    },
    content: {
        paddingVertical: spacing.sm,
    },
    contentEmpty: {
        flexGrow: 1,
        justifyContent: 'center',
    },

    // Empty state — no session
    empty: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        padding: spacing.xl,
        gap: spacing.sm,
    },
    emptyIcon: {
        fontSize: 40,
    },
    emptyTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: colors.gray[800],
    },
    emptySubtitle: {
        fontSize: 13,
        color: colors.gray[400],
        textAlign: 'center',
        maxWidth: 260,
        lineHeight: 20,
    },
    emptyMessages: {
        alignItems: 'center',
        padding: spacing.lg,
    },

    // Loading
    loading: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
    },

    // Streaming
    streamingText: {
        fontSize: 15,
        color: colors.gray[800],
        lineHeight: 22,
    },
    typingContainer: {
        paddingHorizontal: spacing.md,
        paddingVertical: spacing.xs,
    },
});
