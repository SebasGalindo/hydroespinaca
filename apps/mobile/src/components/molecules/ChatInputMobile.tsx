import React, { useRef, useState, useCallback } from 'react';
import {
    View,
    TextInput,
    TouchableOpacity,
    StyleSheet,
    Platform,
    ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { colors, spacing, borderRadius } from '@hydroespinaca/shared';

interface ChatInputMobileProps {
    /** Called when the user submits a message */
    onSend: (message: string) => void;
    /** When true the input and button are disabled */
    disabled?: boolean;
    /** Placeholder text override */
    placeholder?: string;
}

/**
 * Molecule — textarea + send button adapted for mobile.
 *
 * - Multiline TextInput that auto-expands up to a maximum height.
 * - Send button shows a loading spinner while `disabled` (streaming).
 * - Bottom padding respects iOS home indicator via Platform.OS check.
 * - Completely controlled — state managed internally; parent receives
 *   the trimmed message via `onSend` callback.
 */
export const ChatInputMobile = React.memo(function ChatInputMobile({
    onSend,
    disabled = false,
    placeholder,
}: ChatInputMobileProps): React.ReactElement {
    const [text, setText] = useState('');
    const inputRef = useRef<TextInput>(null);

    const handleSend = useCallback(() => {
        const trimmed = text.trim();
        if (!trimmed || disabled) return;
        onSend(trimmed);
        setText('');
    }, [text, disabled, onSend]);

    const canSend = text.trim().length > 0 && !disabled;

    const resolvedPlaceholder =
        placeholder ??
        (disabled ? 'Esperando respuesta del asistente…' : 'Escribe tu pregunta…');

    return (
        <View style={styles.container}>
            <TextInput
                ref={inputRef}
                value={text}
                onChangeText={setText}
                placeholder={resolvedPlaceholder}
                placeholderTextColor={colors.gray[400]}
                multiline
                scrollEnabled
                style={styles.textInput}
                editable={!disabled}
                returnKeyType="default"
                blurOnSubmit={false}
                accessibilityLabel="Campo de mensaje al asistente"
            />

            <TouchableOpacity
                id="chat-send-button-mobile"
                style={[styles.sendButton, !canSend && styles.sendButtonDisabled]}
                onPress={handleSend}
                disabled={!canSend}
                accessibilityLabel="Enviar mensaje"
                accessibilityRole="button"
            >
                {disabled ? (
                    <ActivityIndicator size="small" color={colors.white} />
                ) : (
                    <Ionicons
                        name="send"
                        size={18}
                        color={canSend ? colors.white : colors.gray[400]}
                    />
                )}
            </TouchableOpacity>
        </View>
    );
});

const styles = StyleSheet.create({
    container: {
        flexDirection: 'row',
        alignItems: 'flex-end',
        paddingHorizontal: spacing.md,
        paddingTop: spacing.sm,
        paddingBottom: Platform.OS === 'ios' ? spacing.lg : spacing.sm,
        borderTopWidth: 1,
        borderTopColor: colors.gray[200],
        backgroundColor: colors.white,
        gap: spacing.sm,
    },
    textInput: {
        flex: 1,
        minHeight: 44,
        maxHeight: 120,
        backgroundColor: colors.gray[50],
        borderRadius: borderRadius['2xl'],
        borderWidth: 1,
        borderColor: colors.gray[200],
        paddingHorizontal: spacing.md,
        paddingTop: 11,
        paddingBottom: 11,
        fontSize: 15,
        color: colors.gray[800],
        lineHeight: 20,
    },
    sendButton: {
        width: 44,
        height: 44,
        borderRadius: 22,
        backgroundColor: colors.hidro[600],
        alignItems: 'center',
        justifyContent: 'center',
        marginBottom: 0,
        flexShrink: 0,
    },
    sendButtonDisabled: {
        backgroundColor: colors.gray[200],
    },
});
