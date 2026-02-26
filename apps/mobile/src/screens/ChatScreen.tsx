import React, { useState, useEffect } from 'react';
import {
    SafeAreaView,
    View,
    Text,
    TouchableOpacity,
    KeyboardAvoidingView,
    Platform,
    StyleSheet,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import {
    useChatStore,
    useChatSSE,
    colors,
    spacing,
    borderRadius,
} from '@hydroespinaca/shared';
import { ChatMessageList } from '../components/organisms/ChatMessageList';
import { ChatInputMobile } from '../components/molecules/ChatInputMobile';
import { ChatSessionsModal } from '../components/organisms/ChatSessionsModal';

const BR = borderRadius as Record<string, number>;

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
    const activeTitle =
        sessions.find((s) => s.id === activeSessionId)?.title ?? 'Asistente IA';

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
                        onPress={() => setShowSessions(true)}
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
                            <Text style={styles.headerTitle} numberOfLines={1}>
                                {activeTitle}
                            </Text>
                            {activeSessionId && (
                                <Text style={styles.headerSubtitle}>
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
                onClose={() => setShowSessions(false)}
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
        borderRadius: BR['lg'] ?? 10,
        backgroundColor: colors.hidro[50],
        alignItems: 'center',
        justifyContent: 'center',
    },
    headerTextCol: {
        flex: 1,
        minWidth: 0,
    },
    headerTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: colors.gray[900],
    },
    headerSubtitle: {
        fontSize: 12,
        color: colors.hidro[500],
        marginTop: 1,
    },
});
