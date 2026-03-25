import React from 'react';
import { View, StyleSheet } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import type { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { getVariableColor, getVariableUnit } from '../../utils/actuatorMapper';

interface SummaryCardsProps {
  variables: EnvironmentalVariableAggregate[];
}

function formatValue(value: number): string {
  return value.toLocaleString('es-ES', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

export function SummaryCards({ variables }: SummaryCardsProps): React.ReactElement | null {
  if (variables.length === 0) return null;

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textPrimary} style={styles.heading}>
        Resumen de Variables
      </Text>
      <View style={styles.grid}>
        {variables.map((variable) => {
          const unit = getVariableUnit(variable.variableName);
          const dotColor = getVariableColor(variable.variableName);

          return (
            <View key={variable.variableCode || variable.variableName} style={styles.card}>
              {/* Header */}
              <View style={styles.cardHeader}>
                <View style={[styles.dot, { backgroundColor: dotColor }]} />
                <Text
                  variant="caption"
                  color={semanticColors.textSecondary}
                  numberOfLines={1}
                  style={styles.cardLabel}
                >
                  {variable.variableName}
                </Text>
              </View>

              {/* Average */}
              <Text variant="h3" color={semanticColors.textPrimary} style={styles.avgValue}>
                {formatValue(variable.summary.avg)}
                {unit ? (
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {' '}{unit}
                  </Text>
                ) : null}
              </Text>
              <Text variant="caption" color={semanticColors.textTertiary}>
                Promedio
              </Text>

              {/* Min / Max */}
              <View style={styles.minMaxRow}>
                <View>
                  <Text variant="caption" color={semanticColors.textTertiary} style={styles.minMaxLabel}>
                    Mín
                  </Text>
                  <Text variant="caption" color={semanticColors.textPrimary} style={styles.minMaxValue}>
                    {formatValue(variable.summary.min)}
                  </Text>
                </View>
                <View style={styles.minMaxRight}>
                  <Text variant="caption" color={semanticColors.textTertiary} style={styles.minMaxLabel}>
                    Máx
                  </Text>
                  <Text variant="caption" color={semanticColors.textPrimary} style={styles.minMaxValue}>
                    {formatValue(variable.summary.max)}
                  </Text>
                </View>
              </View>

              {/* Count badge */}
              <View style={styles.countRow}>
                <Text variant="caption" color={semanticColors.textTertiary}>
                  Lecturas
                </Text>
                <View style={styles.countBadge}>
                  <Text variant="caption" color={colors.hidro[800]} style={styles.countText}>
                    {variable.summary.count.toLocaleString()}
                  </Text>
                </View>
              </View>
            </View>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.sm,
  },
  heading: {
    fontWeight: typography.fontWeight.semibold,
    marginHorizontal: spacing.md,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: spacing.md,
    gap: spacing.sm,
  },
  card: {
    width: '48%',
    flexGrow: 1,
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    padding: spacing.md,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginBottom: spacing.sm,
  },
  dot: {
    width: 10,
    height: 10,
    borderRadius: borderRadius.md,
  },
  cardLabel: {
    flex: 1,
  },
  avgValue: {
    fontWeight: typography.fontWeight.bold,
  },
  minMaxRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.gray[100],
  },
  minMaxRight: {
    alignItems: 'flex-end',
  },
  minMaxLabel: {
    fontSize: typography.fontSize.xxs,
  },
  minMaxValue: {
    fontWeight: typography.fontWeight.semibold,
  },
  countRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.gray[100],
  },
  countBadge: {
    backgroundColor: colors.hidro[100],
    paddingHorizontal: spacing.xs,
    paddingVertical: 2,
    borderRadius: borderRadius.sm,
  },
  countText: {
    fontSize: typography.fontSize.xxs,
    fontWeight: typography.fontWeight.semibold,
  },
});
