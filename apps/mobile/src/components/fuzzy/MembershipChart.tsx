/**
 * MembershipChart — Gráfico de funciones de membresía.
 * Usa react-native-gifted-charts LineChart para renderizar
 * todas las MFs de una variable.
 */
import React, { useMemo } from 'react';
import { View, StyleSheet } from 'react-native';
import { LineChart } from 'react-native-gifted-charts';
import { Text } from '../atoms/Text';
import type { FuzzyTerm } from '@hydroespinaca/shared';
import { colors, spacing, borderRadius, semanticColors, typography, chartColors } from '@hydroespinaca/shared';
import { evaluateMF, CHART_COLORS } from '../../utils/fuzzyHelpers';

export interface MembershipChartProps {
  terms: FuzzyTerm[];
  universeMin?: number;
  universeMax?: number;
  height?: number;
  highlightTermId?: string | null;
}

interface DataPoint {
  value: number;
  label?: string;
  hideDataPoint?: boolean;
}

export function MembershipChart({
  terms,
  universeMin: propMin,
  universeMax: propMax,
  height = 180,
  highlightTermId,
}: MembershipChartProps): React.ReactElement {
  const chartData = useMemo(() => {
    if (terms.length === 0) return { datasets: [], labels: [] };

    // Determine universe range
    let uMin = propMin ?? Infinity;
    let uMax = propMax ?? -Infinity;
    for (const t of terms) {
      if (t.membershipFunction.universeMin < uMin) uMin = t.membershipFunction.universeMin;
      if (t.membershipFunction.universeMax > uMax) uMax = t.membershipFunction.universeMax;
    }
    if (!isFinite(uMin)) uMin = 0;
    if (!isFinite(uMax)) uMax = 100;

    const numPoints = 60;
    const step = (uMax - uMin) / numPoints;

    const datasets: { data: DataPoint[]; color: string; label: string }[] = [];

    terms.forEach((term, index) => {
      const { functionType, parameters } = term.membershipFunction;
      const colorIdx = index % CHART_COLORS.length;
      const data: DataPoint[] = [];

      for (let i = 0; i <= numPoints; i++) {
        const x = uMin + i * step;
        const y = evaluateMF(functionType, parameters, x);
        data.push({
          value: Math.round(y * 1000) / 1000,
          label: i % 10 === 0 ? `${Math.round(x * 10) / 10}` : '',
          hideDataPoint: true,
        });
      }

      datasets.push({
        data,
        color: CHART_COLORS[colorIdx]!,
        label: term.label,
      });
    });

    return { datasets, uMin, uMax };
  }, [terms, propMin, propMax]);

  if (terms.length === 0) {
    return (
      <View style={[styles.emptyContainer, { height }]}>
        <Text variant="caption" color={semanticColors.textTertiary}>
          No hay términos definidos
        </Text>
      </View>
    );
  }

  const { datasets } = chartData;

  // Use the first dataset as primary, others as secondary lines
  const primaryData = datasets[0]?.data ?? [];
  const secondaryDataSets = datasets.slice(1).map((ds) => ({
    data: ds.data,
    color: ds.color,
    thickness: highlightTermId ? (datasets.find((d) => d.label === highlightTermId) ? 3 : 1) : 2,
  }));

  return (
    <View style={styles.container}>
      <LineChart
        data={primaryData}
        data2={datasets[1]?.data}
        data3={datasets[2]?.data}
        data4={datasets[3]?.data}
        data5={datasets[4]?.data}
        height={height}
        width={280}
        maxValue={1.05}
        noOfSections={4}
        color={datasets[0]?.color ?? colors.hidro[500]}
        color2={datasets[1]?.color ?? colors.error[500]}
        color3={datasets[2]?.color ?? colors.warning[500]}
        color4={datasets[3]?.color ?? colors.info[500]}
        color5={datasets[4]?.color ?? chartColors.violet}
        thickness={2}
        hideDataPoints
        curved
        yAxisTextStyle={styles.axisText}
        xAxisLabelTextStyle={styles.axisText}
        rulesColor={colors.gray[200]}
        yAxisColor={colors.gray[300]}
        xAxisColor={colors.gray[300]}
        backgroundColor="transparent"
        areaChart={false}
        spacing={280 / (primaryData.length || 1)}
        initialSpacing={0}
        endSpacing={0}
        yAxisLabelSuffix=""
        hideRules={false}
        adjustToWidth
      />

      {/* Legend */}
      <View style={styles.legend}>
        {datasets.map((ds, i) => (
          <View key={i} style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: ds.color }]} />
            <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={1}>
              {ds.label}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.sm,
  },
  emptyContainer: {
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
    justifyContent: 'center',
    alignItems: 'center',
  },
  axisText: {
    fontSize: typography.fontSize.micro,
    color: semanticColors.textTertiary,
  },
  legend: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    marginTop: spacing.xs,
    justifyContent: 'center',
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  legendDot: {
    width: 8,
    height: 8,
    borderRadius: borderRadius.sm,
  },
});
