import React, { useEffect, useRef } from 'react';
import { View, Animated, StyleSheet, ViewStyle, DimensionValue, StyleProp } from 'react-native';
import { colors, spacing, borderRadius, semanticColors } from '@hydroespinaca/shared';

export interface SkeletonLoaderProps {
  /** Presets: 'card', 'list-item', 'stat', 'text' */
  variant?: 'card' | 'list-item' | 'stat' | 'text';
  /** Número de items skeleton a renderizar */
  count?: number;
  /** Ancho personalizado */
  width?: DimensionValue;
  /** Alto personalizado */
  height?: DimensionValue;
  style?: ViewStyle;
  testID?: string;
}

function SkeletonPulse({ style }: { style?: StyleProp<ViewStyle> }): React.ReactElement {
  const opacity = useRef(new Animated.Value(0.3)).current;

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.7,
          duration: 800,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 0.3,
          duration: 800,
          useNativeDriver: true,
        }),
      ])
    );
    animation.start();
    return () => animation.stop();
  }, [opacity]);

  return (
    <Animated.View
      style={[
        styles.pulse,
        { opacity },
        style,
      ]}
    />
  );
}

function SkeletonCard(): React.ReactElement {
  return (
    <View style={styles.card}>
      <SkeletonPulse style={styles.cardHeader} />
      <SkeletonPulse style={styles.cardBody} />
      <SkeletonPulse style={styles.cardFooter} />
    </View>
  );
}

function SkeletonListItem(): React.ReactElement {
  return (
    <View style={styles.listItem}>
      <SkeletonPulse style={styles.listAvatar} />
      <View style={styles.listText}>
        <SkeletonPulse style={styles.listTitle} />
        <SkeletonPulse style={styles.listSubtitle} />
      </View>
    </View>
  );
}

function SkeletonStat(): React.ReactElement {
  return (
    <View style={styles.stat}>
      <SkeletonPulse style={styles.statLabel} />
      <SkeletonPulse style={styles.statValue} />
    </View>
  );
}

function SkeletonText(): React.ReactElement {
  return (
    <View style={styles.textBlock}>
      <SkeletonPulse style={styles.textLine} />
      <SkeletonPulse style={[styles.textLine, { width: '70%' }]} />
    </View>
  );
}

export function SkeletonLoader({
  variant = 'card',
  count = 1,
  width,
  height,
  style,
  testID,
}: SkeletonLoaderProps): React.ReactElement {
  // Custom size skeleton
  if (width || height) {
    return (
      <View style={style} testID={testID}>
        {Array.from({ length: count }).map((_, i) => (
          <SkeletonPulse
            key={i}
            style={{
              width: width || '100%',
              height: height || 20,
              marginBottom: i < count - 1 ? spacing.sm : 0,
            }}
          />
        ))}
      </View>
    );
  }

  const renderVariant = () => {
    switch (variant) {
      case 'card': return <SkeletonCard />;
      case 'list-item': return <SkeletonListItem />;
      case 'stat': return <SkeletonStat />;
      case 'text': return <SkeletonText />;
    }
  };

  return (
    <View style={style} testID={testID}>
      {Array.from({ length: count }).map((_, i) => (
        <View key={i} style={i < count - 1 ? { marginBottom: spacing.md } : undefined}>
          {renderVariant()}
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  pulse: {
    backgroundColor: colors.gray[200],
    borderRadius: borderRadius.sm,
  },
  // Card
  card: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
    gap: spacing.sm,
  },
  cardHeader: {
    width: '60%',
    height: 16,
  },
  cardBody: {
    width: '100%',
    height: 40,
  },
  cardFooter: {
    width: '40%',
    height: 12,
  },
  // List item
  listItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    gap: spacing.md,
  },
  listAvatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
  },
  listText: {
    flex: 1,
    gap: spacing.xs,
  },
  listTitle: {
    width: '70%',
    height: 14,
  },
  listSubtitle: {
    width: '50%',
    height: 12,
  },
  // Stat
  stat: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
    gap: spacing.sm,
  },
  statLabel: {
    width: '50%',
    height: 12,
  },
  statValue: {
    width: '30%',
    height: 24,
  },
  // Text
  textBlock: {
    gap: spacing.xs,
  },
  textLine: {
    width: '100%',
    height: 14,
  },
});
