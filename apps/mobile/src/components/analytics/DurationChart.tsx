import React from 'react';
import { View, StyleSheet, Dimensions } from 'react-native';
import { BarChart } from 'react-native-gifted-charts';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { type ActuatorActivity, getActuatorColor } from '../../utils/actuatorMapper';

interface DurationChartProps {
  data: ActuatorActivity[];
}

const SCREEN_WIDTH = Dimensions.get('window').width;

export function DurationChart({ data }: DurationChartProps): React.ReactElement | null {
  if (data.length === 0) return null;

  const barData = data.map((a) => ({
    value: a.totalDuration / 60, // hours
    label: a.actuatorName.length > 8
      ? a.actuatorName.substring(0, 7) + '…'
      : a.actuatorName,
    frontColor: getActuatorColor(a.actuatorId),
    topLabelComponent: () => (
      <Text variant="caption" color={semanticColors.textSecondary} style={styles.barLabel}>
        {(a.totalDuration / 60).toFixed(1)}h
      </Text>
    ),
  }));

  const maxValue = Math.max(...data.map((a) => a.totalDuration / 60));
  const barWidth = Math.min(
    40,
    Math.max(20, (SCREEN_WIDTH - 120) / data.length - 12),
  );

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textPrimary} style={styles.heading}>
        Duración Total por Actuador
      </Text>

      <View style={styles.chartWrapper}>
        <BarChart
          data={barData}
          barWidth={barWidth}
          spacing={12}
          roundedTop
          roundedBottom={false}
          noOfSections={4}
          maxValue={maxValue * 1.2 || 1}
          yAxisTextStyle={styles.axisText}
          xAxisLabelTextStyle={styles.xAxisLabel}
          yAxisColor={colors.gray[200]}
          xAxisColor={colors.gray[200]}
          hideRules={false}
          rulesColor={colors.gray[100]}
          isAnimated
          animationDuration={500}
          yAxisLabelSuffix="h"
          width={SCREEN_WIDTH - 100}
        />
      </View>

      {/* Activation counts */}
      <View style={styles.countsGrid}>
        {data.map((a) => (
          <View key={a.actuatorId} style={styles.countItem}>
            <View
              style={[styles.countDot, { backgroundColor: getActuatorColor(a.actuatorId) }]}
            />
            <View style={styles.countTextWrapper}>
              <Text
                variant="caption"
                color={semanticColors.textPrimary}
                numberOfLines={1}
                style={styles.countName}
              >
                {a.actuatorName}
              </Text>
              <Text variant="caption" color={semanticColors.textTertiary}>
                {a.activationCount} activaciones
              </Text>
            </View>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    padding: spacing.md,
    marginHorizontal: spacing.md,
    gap: spacing.sm,
  },
  heading: {
    fontWeight: typography.fontWeight.semibold,
  },
  chartWrapper: {
    alignItems: 'center',
    paddingTop: spacing.sm,
  },
  axisText: {
    fontSize: typography.fontSize.xxs,
    color: colors.gray[500],
  },
  xAxisLabel: {
    fontSize: typography.fontSize.micro,
    color: colors.gray[500],
    width: 60,
    textAlign: 'center',
  },
  barLabel: {
    fontSize: typography.fontSize.micro,
    fontWeight: typography.fontWeight.semibold,
    textAlign: 'center',
    marginBottom: 2,
  },
  countsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.gray[100],
  },
  countItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    width: '48%',
  },
  countDot: {
    width: 10,
    height: 10,
    borderRadius: borderRadius.md,
  },
  countTextWrapper: {
    flex: 1,
  },
  countName: {
    fontWeight: typography.fontWeight.medium,
    fontSize: typography.fontSize.xxs,
  },
});
