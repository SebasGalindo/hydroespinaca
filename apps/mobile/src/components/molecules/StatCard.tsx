import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius, shadows, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';

export interface StatCardProps {
  label: string;
  value: string | number;
  icon?: string;
  iconColor?: string;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: string;
  style?: ViewStyle;
  testID?: string;
}

export function StatCard({
  label,
  value,
  icon,
  iconColor = semanticColors.primary,
  trend,
  trendValue,
  style,
  testID,
}: StatCardProps): React.ReactElement {
  const getTrendColor = () => {
    switch (trend) {
      case 'up': return colors.hidro[600];
      case 'down': return colors.error[600];
      default: return colors.gray[500];
    }
  };

  const getTrendIcon = () => {
    switch (trend) {
      case 'up': return 'trending-up';
      case 'down': return 'trending-down';
      default: return 'minus';
    }
  };

  return (
    <View style={[styles.container, style]} testID={testID}>
      <View style={styles.header}>
        {icon && (
          <View style={[styles.iconContainer, { backgroundColor: `${iconColor}15` }]}>
            <Icon name={icon as any} size={18} color={iconColor} />
          </View>
        )}
        <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={1} style={styles.label}>
          {label}
        </Text>
      </View>
      <Text variant="h2" color={semanticColors.textPrimary} style={styles.value}>
        {value}
      </Text>
      {trend && trendValue && (
        <View style={styles.trendContainer}>
          <Icon name={getTrendIcon() as any} size={14} color={getTrendColor()} />
          <Text variant="caption" color={getTrendColor()}>
            {trendValue}
          </Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginBottom: spacing.sm,
  },
  iconContainer: {
    width: 28,
    height: 28,
    borderRadius: borderRadius.lg,
    justifyContent: 'center',
    alignItems: 'center',
  },
  label: {
    flex: 1,
  },
  value: {
    fontWeight: typography.fontWeight.bold,
  },
  trendContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginTop: spacing.xs,
  },
});
