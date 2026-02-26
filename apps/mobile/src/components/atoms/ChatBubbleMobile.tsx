import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius } from '@hydroespinaca/shared';

export interface ChatBubbleMobileProps {
    /** Message author — controls visual alignment and colour */
    role: 'user' | 'model' | 'system';
    /** Content to render inside the bubble */
    children: React.ReactNode;
    /** When true, adds a left border indicator on model bubbles */
    isStreaming?: boolean;
    /** Additional container styles */
    style?: ViewStyle;
}

/**
 * Atom — base visual container for a single chat message (React Native).
 *
 * User bubbles: right-aligned with primary brand green background.
 * Model bubbles: left-aligned with light gray background.
 * System: centred, transparent (child handles typography).
 *
 * Contains NO business logic — use ChatMessageItem / StreamingMessage above it.
 */
export function ChatBubbleMobile({
    role,
    children,
    isStreaming = false,
    style,
}: ChatBubbleMobileProps): React.ReactElement {
    return (
        <View
            style={[
                styles.wrapper,
                role === 'user'
                    ? styles.wrapperUser
                    : role === 'system'
                        ? styles.wrapperSystem
                        : styles.wrapperModel,
                style,
            ]}
        >
            <View
                style={[
                    styles.bubble,
                    role === 'user'
                        ? styles.bubbleUser
                        : role === 'system'
                            ? styles.bubbleSystem
                            : [styles.bubbleModel, isStreaming ? styles.bubbleStreaming : null],
                ]}
            >
                {children}
            </View>
        </View>
    );
}

const BORDER_RADIUS = (borderRadius as Record<string, number>)['xl'] ?? 20;

const styles = StyleSheet.create({
    wrapper: {
        flexDirection: 'row',
        paddingVertical: 4,
        paddingHorizontal: spacing.sm,
    },
    wrapperUser: {
        justifyContent: 'flex-end',
    },
    wrapperModel: {
        justifyContent: 'flex-start',
    },
    wrapperSystem: {
        justifyContent: 'center',
    },

    bubble: {
        maxWidth: '82%',
        paddingHorizontal: 14,
        paddingVertical: 10,
        borderRadius: BORDER_RADIUS,
    },
    bubbleUser: {
        backgroundColor: colors.hidro[600], // #16a34a — brand green
        borderBottomRightRadius: 4,
    },
    bubbleModel: {
        backgroundColor: colors.gray[50],
        borderWidth: 1,
        borderColor: colors.gray[200],
        borderBottomLeftRadius: 4,
    },
    bubbleSystem: {
        backgroundColor: 'transparent',
        maxWidth: '100%',
    },
    bubbleStreaming: {
        borderLeftWidth: 3,
        borderLeftColor: colors.hidro[400], // #4ade80
    },
});
