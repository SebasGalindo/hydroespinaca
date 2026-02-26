import React, { useRef } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Animated } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import { Ionicons } from '@expo/vector-icons';
import type { ChatSession } from '@hydroespinaca/shared';
import { colors, spacing, borderRadius } from '@hydroespinaca/shared';

interface ChatSessionItemMobileProps {
    session: ChatSession;
    isActive: boolean;
    onSelect: (id: string) => void;
    onDelete: (id: string) => void;
}

/**
 * Molecule — a single session row for the mobile session modal.
 *
 * Swipe left to reveal the "Eliminar" action.
 * Tapping the row selects the session.
 */
export function ChatSessionItemMobile({
    session,
    isActive,
    onSelect,
    onDelete,
}: ChatSessionItemMobileProps): React.ReactElement {
    const swipeRef = useRef<Swipeable>(null);

    const handleDelete = () => {
        swipeRef.current?.close();
        onDelete(session.id);
    };

    const renderRightActions = (
        _progress: Animated.AnimatedInterpolation<number>,
        dragX: Animated.AnimatedInterpolation<number>
    ) => {
        const scale = dragX.interpolate({
            inputRange: [-80, 0],
            outputRange: [1, 0],
            extrapolate: 'clamp',
        });

        return (
            <TouchableOpacity
                style={styles.deleteAction}
                onPress={handleDelete}
                accessibilityLabel={`Eliminar conversación: ${session.title}`}
            >
                <Animated.View style={{ transform: [{ scale }] }}>
                    <Ionicons name="trash-outline" size={20} color={colors.white} />
                </Animated.View>
                <Text style={styles.deleteText}>Eliminar</Text>
            </TouchableOpacity>
        );
    };

    const dateLabel = formatRelative(session.updatedAt);

    return (
        <Swipeable
            ref={swipeRef}
            renderRightActions={renderRightActions}
            rightThreshold={40}
            friction={2}
        >
            <TouchableOpacity
                style={[styles.row, isActive && styles.activeRow]}
                onPress={() => onSelect(session.id)}
                accessibilityRole="button"
                accessibilityLabel={`Conversación: ${session.title}`}
                accessibilityState={{ selected: isActive }}
            >
                {/* Icon */}
                <View style={[styles.iconContainer, isActive && styles.activeIconContainer]}>
                    <Ionicons
                        name="chatbubble-ellipses-outline"
                        size={18}
                        color={isActive ? colors.hidro[600] : colors.gray[400]}
                    />
                </View>

                {/* Content */}
                <View style={styles.content}>
                    <Text
                        style={[styles.title, isActive && styles.activeTitle]}
                        numberOfLines={1}
                    >
                        {session.title || 'Nueva conversación'}
                    </Text>
                    <Text style={styles.date}>{dateLabel}</Text>
                </View>

                {/* Chevron */}
                <Ionicons
                    name="chevron-forward"
                    size={14}
                    color={isActive ? colors.hidro[400] : colors.gray[300]}
                />
            </TouchableOpacity>
        </Swipeable>
    );
}

function formatRelative(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60_000);
    if (mins < 1) return 'Ahora';
    if (mins < 60) return `${mins} min`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs} h`;
    const days = Math.floor(hrs / 24);
    if (days < 7) return `${days} d`;
    if (days < 30) return `${Math.floor(days / 7)} sem`;
    return `${Math.floor(days / 30)} mes`;
}

const styles = StyleSheet.create({
    row: {
        flexDirection: 'row',
        alignItems: 'center',
        paddingHorizontal: spacing.md,
        paddingVertical: spacing.sm + 2,
        backgroundColor: colors.white,
        borderBottomWidth: 1,
        borderBottomColor: colors.gray[100],
        gap: spacing.sm,
    },
    activeRow: {
        backgroundColor: colors.hidro[50],
    },
    iconContainer: {
        width: 36,
        height: 36,
        borderRadius: (borderRadius as Record<string, number>)['lg'] ?? 10,
        backgroundColor: colors.gray[100],
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
    },
    activeIconContainer: {
        backgroundColor: colors.hidro[100],
    },
    content: {
        flex: 1,
        minWidth: 0,
    },
    title: {
        fontSize: 15,
        fontWeight: '500',
        color: colors.gray[800],
    },
    activeTitle: {
        color: colors.hidro[700],
        fontWeight: '600',
    },
    date: {
        fontSize: 12,
        color: colors.gray[400],
        marginTop: 1,
    },
    deleteAction: {
        backgroundColor: colors.error[500],
        justifyContent: 'center',
        alignItems: 'center',
        width: 80,
        gap: 4,
    },
    deleteText: {
        color: colors.white,
        fontSize: 11,
        fontWeight: '600',
    },
});
