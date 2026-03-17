import React, { useCallback } from 'react';
import {
    View,
    FlatList,
    TouchableOpacity,
    Modal,
    ActivityIndicator,
    StyleSheet,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useChatStore, colors, spacing, borderRadius, typography } from '@hydroespinaca/shared';
import type { ChatSession } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { ChatSessionItemMobile } from '../molecules/ChatSessionItemMobile';

interface ChatSessionsModalProps {
    /** Whether the modal is visible */
    visible: boolean;
    /** Called to dismiss the modal */
    onClose: () => void;
}

const keyExtractor = (s: ChatSession) => s.id;

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
export const ChatSessionsModal = React.memo(function ChatSessionsModal({
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

    const handleSelect = useCallback((id: string) => {
        setActiveSession(id);
        onClose();
    }, [setActiveSession, onClose]);

    const handleNew = useCallback(async () => {
        try {
            await createSession();
            onClose();
        } catch {
            // createSessionError is handled by the store
        }
    }, [createSession, onClose]);

    const renderItem = useCallback(({ item }: { item: ChatSession }) => (
        <ChatSessionItemMobile
            session={item}
            isActive={item.id === activeSessionId}
            onSelect={handleSelect}
            onDelete={deleteSession}
        />
    ), [activeSessionId, handleSelect, deleteSession]);

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

                    <Text variant="body" weight="semibold" color={colors.gray[900]} style={styles.headerTitle}>
                        Conversaciones
                    </Text>

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
                                <Text variant="caption" weight="medium" color={colors.hidro[600]}>
                                    Nueva
                                </Text>
                            </>
                        )}
                    </TouchableOpacity>
                </View>

                {/* ── Session list ────────────────────────────────── */}
                {sessionsLoading ? (
                    <View style={styles.loadingContainer}>
                        <ActivityIndicator size="large" color={colors.hidro[600]} />
                        <Text variant="caption" color={colors.gray[400]}>
                            Cargando conversaciones…
                        </Text>
                    </View>
                ) : sessions.length === 0 ? (
                    <View style={styles.emptyContainer}>
                        <Ionicons
                            name="chatbubbles-outline"
                            size={48}
                            color={colors.gray[300]}
                        />
                        <Text variant="body" weight="semibold" color={colors.gray[700]}>
                            Sin conversaciones
                        </Text>
                        <Text variant="caption" color={colors.gray[400]} style={styles.emptySubtitle}>
                            Toca "Nueva" para iniciar tu primera conversación con el asistente.
                        </Text>
                    </View>
                ) : (
                    <FlatList<ChatSession>
                        data={sessions}
                        keyExtractor={keyExtractor}
                        renderItem={renderItem}
                        showsVerticalScrollIndicator={false}
                        contentContainerStyle={styles.listContent}
                    />
                )}
            </SafeAreaView>
        </Modal>
    );
});

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
        flex: 1,
        textAlign: 'center',
    },
    newBtn: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: spacing.xs,
        paddingHorizontal: spacing.sm,
        paddingVertical: spacing.xs,
        borderRadius: borderRadius.lg,
        backgroundColor: colors.hidro[50],
    },
    newBtnDisabled: {
        opacity: 0.5,
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

    // Empty
    emptyContainer: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        paddingHorizontal: spacing.xl,
        gap: spacing.sm,
    },
    emptySubtitle: {
        textAlign: 'center',
        maxWidth: 260,
        lineHeight: typography.lineHeight.relaxed,
    },
});
