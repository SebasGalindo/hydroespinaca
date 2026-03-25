import React, { useEffect, useRef } from 'react';
import { View, StyleSheet, Animated } from 'react-native';
import { spacing, borderRadius, colors, semanticColors } from '@hydroespinaca/shared';

interface VariableCardSkeletonProps {
  count?: number;
}

function SkeletonPulse({ style }: { style?: any }): React.ReactElement {
  const opacity = useRef(new Animated.Value(0.3)).current;

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, { toValue: 0.7, duration: 800, useNativeDriver: true }),
        Animated.timing(opacity, { toValue: 0.3, duration: 800, useNativeDriver: true }),
      ])
    );
    animation.start();
    return () => animation.stop();
  }, [opacity]);

  return <Animated.View style={[styles.pulse, { opacity }, style]} />;
}

function SingleSkeleton(): React.ReactElement {
  return (
    <View style={styles.container}>
      {/* Header row */}
      <View style={styles.header}>
        <SkeletonPulse style={styles.titleBar} />
        <SkeletonPulse style={styles.iconCircle} />
      </View>
      {/* Value */}
      <SkeletonPulse style={styles.valueBar} />
      {/* Optimal range */}
      <SkeletonPulse style={styles.optimalBar} />
    </View>
  );
}

export function VariableCardSkeleton({
  count = 6,
}: VariableCardSkeletonProps): React.ReactElement {
  return (
    <View style={styles.grid}>
      {Array.from({ length: count }).map((_, i) => (
        <View key={i} style={styles.cardWrapper}>
          <SingleSkeleton />
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  cardWrapper: {
    width: '48%',
  },
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderLeftWidth: 4,
    borderLeftColor: colors.gray[200],
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
    minHeight: 130,
  },
  pulse: {
    backgroundColor: colors.gray[200],
    borderRadius: borderRadius.sm,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  titleBar: {
    width: '60%',
    height: 12,
  },
  iconCircle: {
    width: 18,
    height: 18,
    borderRadius: 9,
  },
  valueBar: {
    width: '70%',
    height: 24,
    marginBottom: spacing.xs,
  },
  optimalBar: {
    width: '90%',
    height: 10,
  },
});
