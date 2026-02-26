import React from 'react';
import {
    View,
    Text,
    FlatList,
    TouchableOpacity,
    Modal,
    SafeAreaView,
    ActivityIndicator,
    StyleSheet,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useChatStore, colors, spacing, borderRadius } from '@hydroespinaca/shared';
import type { ChatSession } from '@hydroespinaca/shared';
import { ChatSessionItemMobile } from '../molecules/ChatSessionItemMobile';

interface ChatSessionsModalProps {
    /** Whether the modal is visible */
    visible: boolean;
    /** Called to dismiss the modal */
    onClose: () => void;
}

/**
 * Organism — full-screen modal listing all chat sessions.
 *
 * Opened from the `ChatScreen` header. Allows the user to:
 * - Create a new session (top-right button).
 * - Select an existing session (tap a row).
 * - Delete a session (swipe-left on a row).
 *
 * Selecting or creating a session automatically closes the modal.
 */
export function ChatSessionsModal({
    visible,
    onClose,
}: ChatSessionsModalProps): React.ReactElement {
    const sessions = useChatStore((s) => s.sessions);
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const sessionsLoading = useChatStore((s) => s.sessionsLoading);
    const createSession = useChatStore((s) => s.createSession);
    const setActiveSession = useChatStore((s) => s.setActiveSession);
    const deleteSession = useChatStore((s) => s.deleteSession);
    const createSessionLoading = useChatStore((s) => s.createSessionLoading);

    const handleSelect = (id: string) => {
        setActiveSession(id);
        onClose();
    };

    const handleNew = async () => {
        try {
            await createSession();
            onClose();
        } catch {
            // createSessionError is handled by the store
        }
    };

    return (
        <Modal
            visible={visible}
            animationType="slide"
            presentationStyle="pageSheet"
            onRequestClose={onClose}
        >
            <SafeAreaView style={styles.container}>
                {/* ── Header ──────────────────────────────────────── */}
                <View style={styles.header}>
                    {/* Close button */}
                    <TouchableOpacity
                        onPress={onClose}
                        style={styles.closeBtn}
                        accessibilityLabel="Cerrar lista de conversaciones"
                        accessibilityRole="button"
                    >
                        <Ionicons name="close" size={22} color={colors.gray[600]} />
                    </TouchableOpacity>

                    <Text style={styles.headerTitle}>Conversaciones</Text>

                    {/* New session button */}
                    <TouchableOpacity
                        onPress={handleNew}
                        disabled={createSessionLoading}
                        style={[styles.newBtn, createSessionLoading && styles.newBtnDisabled]}
                        accessibilityLabel="Nueva conversación"
                        accessibilityRole="button"
                    >
                        {createSessionLoading ? (
                            <ActivityIndicator size="small" color={colors.hidro[600]} />
                        ) : (
                            <>
                                <Ionicons name="add" size={18} color={colors.hidro[600]} />
                                <Text style={styles.newBtnText}>Nueva</Text>
                            </>
                        )}
                    </TouchableOpacity>
                </View>

                {/* ── Session list ────────────────────────────────── */}
                {sessionsLoading ? (
                    <View style={styles.loadingContainer}>
                        <ActivityIndicator size="large" color={colors.hidro[600]} />
                        <Text style={styles.loadingText}>Cargando conversaciones…</Text>
                    </View>
                ) : sessions.length === 0 ? (
                    <View style={styles.emptyContainer}>
                        <Ionicons
                            name="chatbubbles-outline"
                            size={48}
                            color={colors.gray[300]}
                        />
                        <Text style={styles.emptyTitle}>Sin conversaciones</Text>
                        <Text style={styles.emptySubtitle}>
                            Toca "Nueva" para iniciar tu primera conversación con el asistente.
                        </Text>
                    </View>
                ) : (
                    <FlatList<ChatSession>
                        data={sessions}
                        keyExtractor={(s) => s.id}
                        renderItem={({ item }) => (
                            <ChatSessionItemMobile
                                session={item}
                                isActive={item.id === activeSessionId}
                                onSelect={handleSelect}
                                onDelete={deleteSession}
                            />
                        )}
                        showsVerticalScrollIndicator={false}
                        contentContainerStyle={styles.listContent}
                    />
                )}
            </SafeAreaView>
        </Modal>
    );
}

const BR = borderRadius as Record<string, number>;

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: colors.white,
    },
    header: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        paddingHorizontal: spacing.md,
        paddingVertical: spacing.md,
        borderBottomWidth: 1,
        borderBottomColor: colors.gray[100],
    },
    closeBtn: {
        padding: spacing.xs,
    },
    headerTitle: {
        fontSize: 17,
        fontWeight: '600',
        color: colors.gray[900],
        flex: 1,
        textAlign: 'center',
    },
    newBtn: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 4,
        paddingHorizontal: spacing.sm + 2,
        paddingVertical: spacing.xs + 2,
        borderRadius: BR['lg'] ?? 10,
        backgroundColor: colors.hidro[50],
    },
    newBtnDisabled: {
        opacity: 0.5,
    },
    newBtnText: {
        fontSize: 14,
        fontWeight: '500',
        color: colors.hidro[600],
    },
    listContent: {
        paddingBottom: spacing.xl,
    },

    // Loading
    loadingContainer: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        gap: spacing.md,
    },
    loadingText: {
        fontSize: 13,
        color: colors.gray[400],
    },

    // Empty
    emptyContainer: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        paddingHorizontal: spacing.xl,
        gap: spacing.sm,
    },
    emptyTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: colors.gray[700],
    },
    emptySubtitle: {
        fontSize: 13,
        color: colors.gray[400],
        textAlign: 'center',
        maxWidth: 260,
        lineHeight: 20,
    },
});
