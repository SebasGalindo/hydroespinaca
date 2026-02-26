import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, spacing } from '@hydroespinaca/shared';

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
export function ChatDateDivider({ date }: ChatDateDividerProps): React.ReactElement {
    const label = getRelativeLabel(date);

    return (
        <View style={styles.container}>
            <View style={styles.line} />
            <Text style={styles.label}>{label}</Text>
            <View style={styles.line} />
        </View>
    );
}

function getRelativeLabel(iso: string): string {
    const now = new Date();
    const d = new Date(iso);
    const diffMs = now.getTime() - d.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Hoy';
    if (diffDays === 1) return 'Ayer';

    return d.toLocaleDateString('es-CO', {
        day: 'numeric',
        month: 'short',
        year: diffDays > 300 ? 'numeric' : undefined,
    });
}

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
        fontSize: 11,
        fontWeight: '600',
        color: colors.gray[400],
        textTransform: 'uppercase',
        letterSpacing: 0.8,
    },
});
