import React, { useEffect, useRef } from 'react';
import { View, StyleSheet, Animated } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { semanticColors, spacing, borderRadius, colors, typography } from '@hydroespinaca/shared';
import type { IconType, IconName } from '@hydroespinaca/shared';

export type TrendDirection = 'up' | 'down' | 'stable';
export type VariableStatus = 'optimal' | 'warning' | 'error' | 'manual';

interface VariableCardProps {
  title: string;
  value: string;
  optimal?: string;
  subtitle?: string;
  iconType: IconType;
  status: VariableStatus;
  trend?: TrendDirection;
  artificialLightActive?: boolean;
  showArtificialLightAlert?: boolean;
  /** Delay index for staggered animation (0-based) */
  animationIndex?: number;
}

export function VariableCard({
  title,
  value,
  optimal,
  subtitle,
  iconType,
  status,
  trend,
  artificialLightActive = false,
  showArtificialLightAlert = false,
  animationIndex = 0,
}: VariableCardProps): React.ReactElement {
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const translateAnim = useRef(new Animated.Value(12)).current;

  useEffect(() => {
    const delay = animationIndex * 80;
    Animated.parallel([
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 350,
        delay,
        useNativeDriver: true,
      }),
      Animated.timing(translateAnim, {
        toValue: 0,
        duration: 350,
        delay,
        useNativeDriver: true,
      }),
    ]).start();
  }, [fadeAnim, translateAnim, animationIndex]);

  const getStatusColor = () => {
    switch (status) {
      case 'optimal':
        return semanticColors.success;
      case 'warning':
        return semanticColors.warning;
      case 'error':
        return colors.error[500];
      case 'manual':
        return semanticColors.info;
      default:
        return semanticColors.textTertiary;
    }
  };

  const getStatusIconName = () => {
    switch (status) {
      case 'optimal':
        return 'check-circle';
      case 'warning':
        return 'alert-triangle';
      case 'error':
        return 'x-circle';
      case 'manual':
        return 'edit';
      default:
        return 'info';
    }
  };

  const getVariableIconName = (): IconName => {
    switch (iconType) {
      case 'temperature':
        return 'thermometer';
      case 'humidity':
        return 'droplet';
      case 'ph':
        return 'activity';
      case 'light':
      case 'sun':
        return 'sun';
      case 'electric':
        return 'zap';
      case 'ruler':
        return 'maximize';
      case 'water':
        return 'droplet';
      default:
        return 'thermometer';
    }
  };

  const getTrendIcon = () => {
    if (!trend || trend === 'stable') return null;

    return (
      <Text
        variant="caption"
        color={trend === 'up' ? semanticColors.success : colors.error[500]}
        style={styles.trendText}
      >
        {trend === 'up' ? '↑' : '↓'}
      </Text>
    );
  };

  return (
    <Animated.View
      style={[
        styles.container,
        { borderLeftWidth: 4, borderLeftColor: getStatusColor() },
        { opacity: fadeAnim, transform: [{ translateY: translateAnim }] },
      ]}
    >
      {/* Header */}
      <View style={styles.header}>
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.title}>
          {title}
        </Text>
        <View style={styles.iconContainer}>
          <Icon
            name={getVariableIconName()}
            size={18}
            color={getStatusColor()}
          />
          {status !== 'manual' && (
            <Icon
              name={getStatusIconName()}
              size={16}
              color={getStatusColor()}
            />
          )}
        </View>
      </View>

      {/* Value */}
      <View style={styles.valueContainer}>
        <Text variant="h2" color={getStatusColor()} style={styles.value}>
          {value}
        </Text>
        {getTrendIcon()}
      </View>

      {/* Optimal range */}
      {optimal && (
        <Text variant="caption" color={getStatusColor()} style={styles.optimal}>
          {optimal}
        </Text>
      )}

      {/* Subtitle */}
      {subtitle && (
        <Text variant="caption" color={semanticColors.textSecondary} style={styles.subtitle}>
          {subtitle}
        </Text>
      )}
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.background,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    shadowColor: colors.black,
    shadowOffset: {
      width: 0,
      height: 1,
    },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
    minHeight: 130,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.sm,
  },
  title: {
    flex: 1,
    fontWeight: typography.fontWeight.medium,
  },
  iconContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  valueContainer: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: spacing.xs,
    marginBottom: spacing.xs,
  },
  value: {
    fontWeight: typography.fontWeight.bold,
  },
  trendText: {
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.bold,
  },
  optimal: {
    marginBottom: spacing.xs,
  },
  subtitle: {
    lineHeight: 16,
  },
  lightAlert: {
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.borderLight,
  },
  lightAlertText: {
    fontWeight: typography.fontWeight.medium,
  },
});
