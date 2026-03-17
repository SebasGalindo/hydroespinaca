import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, spacing, typography, formatRelativeDate } from '@hydroespinaca/shared';

interface ChatDateDividerProps {
    /** ISO 8601 date string */
    date: string;
}

/**
 * Atom — full-width date separator shown between message groups.
 *
 * Displays "Hoy", "Ayer", or a short date in the centre with
 * decorative horizontal lines on both sides.
 */
export const ChatDateDivider = React.memo(function ChatDateDivider({ date }: ChatDateDividerProps): React.ReactElement {
    const label = formatRelativeDate(date);

    return (
        <View style={styles.container}>
            <View style={styles.line} />
            <Text style={styles.label}>{label}</Text>
            <View style={styles.line} />
        </View>
    );
});

const styles = StyleSheet.create({
    container: {
        flexDirection: 'row',
        alignItems: 'center',
        marginVertical: spacing.sm,
        paddingHorizontal: spacing.md,
    },
    line: {
        flex: 1,
        height: 1,
        backgroundColor: colors.gray[200],
    },
    label: {
        marginHorizontal: spacing.sm,
        fontSize: typography.fontSize.xs,
        fontWeight: typography.fontWeight.semibold as any,
        color: colors.gray[400],
        textTransform: 'uppercase',
        letterSpacing: typography.letterSpacing.wide,
    },
});
