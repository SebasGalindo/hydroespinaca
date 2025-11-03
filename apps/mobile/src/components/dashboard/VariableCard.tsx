import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { semanticColors, spacing, borderRadius, colors } from '@hydroespinaca/shared';
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
}: VariableCardProps): React.ReactElement {

  const getStatusColor = () => {
    switch (status) {
      case 'optimal':
        return '#16a34a'; // green-600
      case 'warning':
        return '#f59e0b'; // yellow-600
      case 'error':
        return '#ef4444'; // red-600
      case 'manual':
        return '#3b82f6'; // blue-600
      default:
        return '#6b7280'; // gray-600
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
        color={trend === 'up' ? '#16a34a' : '#ef4444'}
        style={styles.trendText}
      >
        {trend === 'up' ? '↑' : '↓'}
      </Text>
    );
  };

  return (
    <View style={[styles.container, { borderLeftWidth: 4, borderLeftColor: getStatusColor() }]}>
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
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.background,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    shadowColor: '#000',
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
    fontWeight: '500',
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
    fontWeight: 'bold',
  },
  trendText: {
    fontSize: 18,
    fontWeight: 'bold',
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
    fontWeight: '500',
  },
});
