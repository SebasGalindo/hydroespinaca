import React, { useRef, useCallback } from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import ReanimatedSwipeable, {
    type SwipeableMethods,
} from 'react-native-gesture-handler/ReanimatedSwipeable';
import Animated, {
    useAnimatedStyle,
    interpolate,
    Extrapolation,
    type SharedValue,
} from 'react-native-reanimated';
import { Ionicons } from '@expo/vector-icons';
import type { ChatSession } from '@hydroespinaca/shared';
import { colors, spacing, borderRadius, formatRelativeTime } from '@hydroespinaca/shared';

interface ChatSessionItemMobileProps {
    session: ChatSession;
    isActive: boolean;
    onSelect: (id: string) => void;
    onDelete: (id: string) => void;
}

/**
 * Small component so we can call useAnimatedStyle with the SharedValue param.
 */
function DeleteActionIcon({ translation }: { translation: SharedValue<number> }) {
    const animatedStyle = useAnimatedStyle(() => ({
        transform: [
            {
                scale: interpolate(
                    translation.value,
                    [-80, 0],
                    [1, 0],
                    Extrapolation.CLAMP,
                ),
            },
        ],
    }));

    return (
        <Animated.View style={animatedStyle}>
            <Ionicons name="trash-outline" size={20} color={colors.white} />
        </Animated.View>
    );
}

/**
 * Molecule — a single session row for the mobile session modal.
 *
 * Swipe left to reveal the "Eliminar" action.
 * Tapping the row selects the session.
 */
export const ChatSessionItemMobile = React.memo(function ChatSessionItemMobile({
    session,
    isActive,
    onSelect,
    onDelete,
}: ChatSessionItemMobileProps): React.ReactElement {
    const swipeRef = useRef<SwipeableMethods>(null);

    const handleDelete = useCallback(() => {
        swipeRef.current?.close();
        onDelete(session.id);
    }, [onDelete, session.id]);

    const handleSelect = useCallback(() => onSelect(session.id), [onSelect, session.id]);

    const renderRightActions = useCallback((
        _progress: SharedValue<number>,
        translation: SharedValue<number>,
    ) => {
        return (
            <TouchableOpacity
                style={styles.deleteAction}
                onPress={handleDelete}
                accessibilityLabel={`Eliminar conversación: ${session.title}`}
            >
                <DeleteActionIcon translation={translation} />
                <Text style={styles.deleteText}>Eliminar</Text>
            </TouchableOpacity>
        );
    }, [handleDelete, session.title]);

    const dateLabel = formatRelativeTime(session.updatedAt);

    return (
        <ReanimatedSwipeable
            ref={swipeRef}
            renderRightActions={renderRightActions}
            rightThreshold={40}
            friction={2}
        >
            <TouchableOpacity
                style={[styles.row, isActive && styles.activeRow]}
                onPress={handleSelect}
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
        </ReanimatedSwipeable>
    );
});

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
        borderRadius: borderRadius.lg,
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
