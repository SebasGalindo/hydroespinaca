import React, { useEffect, useRef } from 'react';
import { View, StyleSheet, Animated } from 'react-native';
import { colors } from '@hydroespinaca/shared';

interface TypingIndicatorProps {
    /** Whether the indicator should be visible */
    visible: boolean;
}

/**
 * Atom — three pulsing dots shown while the bot is processing a response.
 *
 * Each dot fades in/out with a staggered delay to create a breathing effect.
 * Visible only when `visible` is true (i.e., streaming started but no text yet).
 */
export const TypingIndicator = React.memo(function TypingIndicator({ visible }: TypingIndicatorProps): React.ReactElement | null {
    const dot1 = useRef(new Animated.Value(0.3)).current;
    const dot2 = useRef(new Animated.Value(0.3)).current;
    const dot3 = useRef(new Animated.Value(0.3)).current;
    const animRef = useRef<Animated.CompositeAnimation | null>(null);

    useEffect(() => {
        if (!visible) {
            animRef.current?.stop();
            dot1.setValue(0.3);
            dot2.setValue(0.3);
            dot3.setValue(0.3);
            return;
        }

        const pulse = (dot: Animated.Value, delay: number) =>
            Animated.loop(
                Animated.sequence([
                    Animated.delay(delay),
                    Animated.timing(dot, {
                        toValue: 1,
                        duration: 400,
                        useNativeDriver: true,
                    }),
                    Animated.timing(dot, {
                        toValue: 0.3,
                        duration: 400,
                        useNativeDriver: true,
                    }),
                    Animated.delay(800 - delay),
                ])
            );

        animRef.current = Animated.parallel([
            pulse(dot1, 0),
            pulse(dot2, 200),
            pulse(dot3, 400),
        ]);
        animRef.current.start();

        return () => {
            animRef.current?.stop();
        };
    }, [visible, dot1, dot2, dot3]);

    if (!visible) return null;

    return (
        <View style={styles.container} accessibilityLabel="El asistente está escribiendo…">
            {[dot1, dot2, dot3].map((dot, i) => (
                <Animated.View
                    key={i}
                    style={[styles.dot, { opacity: dot }]}
                />
            ))}
        </View>
    );
});

const styles = StyleSheet.create({
    container: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 4,
        paddingVertical: 4,
    },
    dot: {
        width: 8,
        height: 8,
        borderRadius: 4,
        backgroundColor: colors.hidro[500], // #22c55e
    },
});
