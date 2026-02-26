import React from 'react';
import { Text, StyleSheet, View } from 'react-native';
import Markdown from 'react-native-markdown-display';
import type { ChatMessage } from '@hydroespinaca/shared';
import { colors, spacing } from '@hydroespinaca/shared';
import { ChatBubbleMobile } from '../atoms/ChatBubbleMobile';

interface ChatMessageItemProps {
    message: ChatMessage;
}

/**
 * Molecule — renders a single `ChatMessage` with role-specific formatting.
 *
 * - `user`: plain text in a right-aligned green bubble.
 * - `model`: Markdown-rendered content via `react-native-markdown-display`,
 *   displayed in a left-aligned gray bubble.
 * - `system`: centred muted italic text.
 *
 * Dependency: `react-native-markdown-display` (add to apps/mobile/package.json)
 */
export function ChatMessageItem({ message }: ChatMessageItemProps): React.ReactElement {
    if (message.role === 'user') {
        return (
            <ChatBubbleMobile role="user">
                <Text style={styles.userText}>{message.content}</Text>
            </ChatBubbleMobile>
        );
    }

    if (message.role === 'system') {
        return (
            <View style={styles.systemWrapper}>
                <Text style={styles.systemText}>{message.content}</Text>
            </View>
        );
    }

    // model — Markdown
    return (
        <ChatBubbleMobile role="model">
            <Markdown style={markdownStyles}>{message.content}</Markdown>
        </ChatBubbleMobile>
    );
}

const styles = StyleSheet.create({
    userText: {
        fontSize: 15,
        color: colors.white,
        lineHeight: 22,
    },
    systemWrapper: {
        alignItems: 'center',
        marginVertical: spacing.xs,
        paddingHorizontal: spacing.lg,
    },
    systemText: {
        fontSize: 12,
        color: colors.gray[400],
        fontStyle: 'italic',
        textAlign: 'center',
    },
});

// Markdown styles for react-native-markdown-display
const markdownStyles = StyleSheet.create({
    body: {
        color: colors.gray[800],
        fontSize: 15,
        lineHeight: 22,
    },
    heading1: {
        fontSize: 18,
        fontWeight: '700',
        color: colors.gray[900],
        marginTop: 8,
        marginBottom: 4,
    },
    heading2: {
        fontSize: 16,
        fontWeight: '600',
        color: colors.gray[800],
        marginTop: 6,
        marginBottom: 3,
    },
    heading3: {
        fontSize: 14,
        fontWeight: '600',
        color: colors.gray[700],
        marginTop: 4,
        marginBottom: 2,
    },
    paragraph: {
        marginTop: 2,
        marginBottom: 2,
    },
    code_inline: {
        backgroundColor: colors.gray[200],
        fontFamily: 'monospace',
        fontSize: 12,
        paddingHorizontal: 4,
        paddingVertical: 1,
        borderRadius: 4,
        color: colors.gray[700],
    },
    fence: {
        backgroundColor: colors.gray[900],
        borderRadius: 8,
        padding: spacing.sm,
        marginVertical: 6,
    },
    code_block: {
        color: colors.gray[100],
        fontFamily: 'monospace',
        fontSize: 12,
        lineHeight: 18,
    },
    link: {
        color: colors.hidro[600],
        textDecorationLine: 'underline',
    },
    list_item: {
        flexDirection: 'row',
        marginBottom: 2,
    },
    bullet_list_icon: {
        color: colors.hidro[500],
        marginRight: 6,
    },
    ordered_list_icon: {
        color: colors.hidro[500],
        marginRight: 6,
    },
    blockquote: {
        borderLeftWidth: 3,
        borderLeftColor: colors.hidro[300],
        paddingLeft: 12,
        marginLeft: 0,
        opacity: 0.8,
    },
    hr: {
        backgroundColor: colors.gray[200],
        height: 1,
        marginVertical: 8,
    },
    strong: {
        fontWeight: '700',
    },
    em: {
        fontStyle: 'italic',
    },
});
