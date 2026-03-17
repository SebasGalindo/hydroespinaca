import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
    View,
    TouchableOpacity,
    KeyboardAvoidingView,
    Platform,
    StyleSheet,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import {
    useChatStore,
    useChatSSE,
    colors,
    spacing,
    borderRadius,
    typography,
} from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { ChatMessageList } from '../components/organisms/ChatMessageList';
import { ChatInputMobile } from '../components/molecules/ChatInputMobile';
import { ChatSessionsModal } from '../components/organisms/ChatSessionsModal';



/**
 * Full-screen dedicated chat screen for the mobile app.
 *
 * Equivalent of the web `ChatDrawer` — assembled from the chat organisms +
 * molecules. Accessible from the "Más" tab via `MoreStack` navigation.
 *
 * Layout (top → bottom):
 *  1. Header: active session title + sessions modal trigger.
 *  2. ChatMessageList: virtualised FlatList with streaming support.
 *  3. ChatInputMobile: auto-expanding input + send button.
 */
export function ChatScreen(): React.ReactElement {
    // ── Chat SSE hook (mobile stream) ─────────────────────────────────────
    const { sendMessage, isStreaming } = useChatSSE();

    // ── Store selectors ───────────────────────────────────────────────────
    const fetchSessions = useChatStore((s) => s.fetchSessions);
    const activeSessionId = useChatStore((s) => s.activeSessionId);
    const sessions = useChatStore((s) => s.sessions);

    // ── Local state ───────────────────────────────────────────────────────
    const [showSessions, setShowSessions] = useState(false);

    // Fetch sessions once on mount
    useEffect(() => {
        fetchSessions();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // Derive active session title
    const activeTitle = useMemo(() =>
        sessions.find((s) => s.id === activeSessionId)?.title ?? 'Asistente IA',
        [sessions, activeSessionId]
    );

    const openSessions = useCallback(() => setShowSessions(true), []);
    const closeSessions = useCallback(() => setShowSessions(false), []);

    return (
        <SafeAreaView style={styles.safeArea}>
            <KeyboardAvoidingView
                style={styles.flex}
                behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
                keyboardVerticalOffset={Platform.OS === 'ios' ? 0 : 0}
            >
                {/* ── Header ──────────────────────────────────────────────── */}
                <View style={styles.header}>
                    {/* Session title (tappable to open modal) */}
                    <TouchableOpacity
                        style={styles.headerTitleBtn}
                        onPress={openSessions}
                        accessibilityLabel="Ver conversaciones"
                        accessibilityRole="button"
                    >
                        <View style={styles.headerIconBox}>
                            <Ionicons
                                name="chatbubble-ellipses"
                                size={18}
                                color={colors.hidro[600]}
                            />
                        </View>
                        <View style={styles.headerTextCol}>
                            <Text variant="body" weight="semibold" color={colors.gray[900]} numberOfLines={1} style={styles.headerTitle}>
                                {activeTitle}
                            </Text>
                            {activeSessionId && (
                                <Text variant="caption" color={colors.hidro[500]} style={styles.headerSubtitle}>
                                    {isStreaming ? 'Escribiendo…' : 'En línea'}
                                </Text>
                            )}
                        </View>
                        <Ionicons
                            name="chevron-down"
                            size={16}
                            color={colors.gray[400]}
                        />
                    </TouchableOpacity>
                </View>

                {/* ── Messages ────────────────────────────────────────────── */}
                <ChatMessageList />

                {/* ── Input ───────────────────────────────────────────────── */}
                <ChatInputMobile
                    onSend={sendMessage}
                    disabled={isStreaming || !activeSessionId}
                    placeholder={
                        !activeSessionId
                            ? 'Selecciona o crea una conversación…'
                            : undefined
                    }
                />
            </KeyboardAvoidingView>

            {/* ── Sessions modal ──────────────────────────────────────────── */}
            <ChatSessionsModal
                visible={showSessions}
                onClose={closeSessions}
            />
        </SafeAreaView>
    );
}

const styles = StyleSheet.create({
    safeArea: {
        flex: 1,
        backgroundColor: colors.white,
    },
    flex: {
        flex: 1,
    },

    // Header
    header: {
        paddingHorizontal: spacing.md,
        paddingTop: spacing.sm,
        paddingBottom: spacing.sm,
        borderBottomWidth: 1,
        borderBottomColor: colors.gray[100],
        backgroundColor: colors.white,
    },
    headerTitleBtn: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: spacing.sm,
    },
    headerIconBox: {
        width: 36,
        height: 36,
        borderRadius: borderRadius.lg,
        backgroundColor: colors.hidro[50],
        alignItems: 'center',
        justifyContent: 'center',
    },
    headerTextCol: {
        flex: 1,
        minWidth: 0,
    },
    headerTitle: {
    },
    headerSubtitle: {
        marginTop: 1,
    },
});
