import React from 'react';
import { View, StyleSheet } from 'react-native';
import { PieChart } from 'react-native-gifted-charts';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { type ActuatorActivity, getActuatorColor } from '../../utils/actuatorMapper';

interface ProportionChartProps {
  data: ActuatorActivity[];
}

export function ProportionChart({ data }: ProportionChartProps): React.ReactElement | null {
  if (data.length === 0) return null;

  const totalDuration = data.reduce((sum, a) => sum + a.totalDuration, 0);

  const pieData = data.map((a) => ({
    value: a.totalDuration,
    color: getActuatorColor(a.actuatorId),
    text: `${((a.totalDuration / totalDuration) * 100).toFixed(0)}%`,
    textColor: colors.white,
    textSize: typography.fontSize.xxs,
  }));

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textPrimary} style={styles.heading}>
        Proporción de Tiempo Activo
      </Text>

      <View style={styles.chartRow}>
        <PieChart
          data={pieData}
          radius={90}
          innerRadius={50}
          innerCircleColor={semanticColors.surface}
          centerLabelComponent={() => (
            <View style={styles.centerLabel}>
              <Text variant="h3" color={semanticColors.textPrimary} style={styles.centerValue}>
                {(totalDuration / 60).toFixed(1)}
              </Text>
              <Text variant="caption" color={semanticColors.textTertiary} style={styles.centerUnit}>
                horas
              </Text>
            </View>
          )}
          showText
          textBackgroundRadius={14}
          isAnimated
        />

        {/* Legend */}
        <View style={styles.legend}>
          {data.map((a) => {
            const pct = totalDuration > 0
              ? ((a.totalDuration / totalDuration) * 100).toFixed(1)
              : '0';
            return (
              <View key={a.actuatorId} style={styles.legendItem}>
                <View
                  style={[styles.legendDot, { backgroundColor: getActuatorColor(a.actuatorId) }]}
                />
                <View style={styles.legendTextWrapper}>
                  <Text
                    variant="caption"
                    color={semanticColors.textPrimary}
                    numberOfLines={1}
                    style={styles.legendName}
                  >
                    {a.actuatorName}
                  </Text>
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {pct}% · {(a.totalDuration / 60).toFixed(1)}h
                  </Text>
                </View>
              </View>
            );
          })}
        </View>
      </View>

      {/* Total footer */}
      <View style={styles.footer}>
        <Text variant="caption" color={semanticColors.textSecondary}>
          <Text variant="caption" color={semanticColors.textPrimary} style={styles.footerBold}>
            Total de tiempo activo:{' '}
          </Text>
          {(totalDuration / 60).toFixed(1)} horas ({totalDuration.toFixed(0)} minutos)
        </Text>
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
    gap: spacing.md,
  },
  heading: {
    fontWeight: typography.fontWeight.semibold,
  },
  chartRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  centerLabel: {
    alignItems: 'center',
  },
  centerValue: {
    fontWeight: typography.fontWeight.bold,
  },
  centerUnit: {
    fontSize: typography.fontSize.xxs,
  },
  legend: {
    flex: 1,
    gap: spacing.sm,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  legendDot: {
    width: 10,
    height: 10,
    borderRadius: borderRadius.md,
  },
  legendTextWrapper: {
    flex: 1,
  },
  legendName: {
    fontWeight: typography.fontWeight.medium,
    fontSize: typography.fontSize.xxs,
  },
  footer: {
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
    padding: spacing.sm,
  },
  footerBold: {
    fontWeight: typography.fontWeight.semibold,
  },
});
