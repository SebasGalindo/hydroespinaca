import React, { useState, useMemo } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import type { EnvironmentalVariableAggregate, AggregateVariabilityPoint } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Select } from '../atoms/Select';
import type { SelectOption } from '../atoms/Select';
import { getVariableColor } from '../../utils/actuatorMapper';
import type { ViewMode } from '../../utils/analyticsFilters';

interface BoxplotChartProps {
  variables: EnvironmentalVariableAggregate[];
  viewMode: ViewMode;
}

function formatTimestamp(ts: string, mode: ViewMode): string {
  const d = new Date(ts);
  const month = d.toLocaleString('es-CO', { month: 'short' });
  const day = d.getDate();
  const h = String(d.getHours()).padStart(2, '0');
  const m = String(d.getMinutes()).padStart(2, '0');

  switch (mode) {
    case 'hourly':
      return `${h}:${m}`;
    case 'daily':
      return `${day} ${month}`;
    default:
      return `${day} ${month}`;
  }
}

/**
 * A single boxplot drawn manually.
 * Shows: whiskers (min→max), box (Q1→Q3), median line.
 */
function BoxplotItem({
  point,
  globalMin,
  globalMax,
  barColor,
  label,
  chartHeight,
}: {
  point: AggregateVariabilityPoint;
  globalMin: number;
  globalMax: number;
  barColor: string;
  label: string;
  chartHeight: number;
}): React.ReactElement {
  const range = globalMax - globalMin || 1;

  // Convert a value to a Y position (0 = bottom, chartHeight = top)
  const toY = (val: number) => ((val - globalMin) / range) * chartHeight;

  const minY = toY(point.min);
  const q1Y = toY(point.q1);
  const medianY = toY(point.median);
  const q3Y = toY(point.q3);
  const maxY = toY(point.max);

  const boxBottom = q1Y;
  const boxHeight = Math.max(q3Y - q1Y, 2);

  return (
    <View style={styles.boxplotItemContainer}>
      <View style={[styles.boxplotCanvas, { height: chartHeight }]}>
        {/* Whisker line (min → max) */}
        <View
          style={[
            styles.whisker,
            {
              bottom: minY,
              height: Math.max(maxY - minY, 1),
              backgroundColor: barColor,
            },
          ]}
        />
        {/* Min cap */}
        <View style={[styles.whiskerCap, { bottom: minY - 1, backgroundColor: barColor }]} />
        {/* Max cap */}
        <View style={[styles.whiskerCap, { bottom: maxY - 1, backgroundColor: barColor }]} />
        {/* Box (Q1 → Q3) */}
        <View
          style={[
            styles.box,
            {
              bottom: boxBottom,
              height: boxHeight,
              backgroundColor: `${barColor}40`,
              borderColor: barColor,
            },
          ]}
        />
        {/* Median line */}
        <View
          style={[
            styles.medianLine,
            { bottom: medianY - 1, backgroundColor: barColor },
          ]}
        />
      </View>
      <Text variant="caption" color={semanticColors.textTertiary} style={styles.boxplotLabel}>
        {label}
      </Text>
    </View>
  );
}

export function BoxplotChart({ variables, viewMode }: BoxplotChartProps): React.ReactElement | null {
  const variableOptions: SelectOption[] = useMemo(
    () =>
      variables.map((v) => ({
        label: v.variableName,
        value: v.variableCode,
      })),
    [variables],
  );

  const [selectedCode, setSelectedCode] = useState(
    variables[0]?.variableCode || '',
  );

  const selectedData = useMemo(
    () => variables.find((v) => v.variableCode === selectedCode),
    [variables, selectedCode],
  );

  // Only show for hourly/daily
  if (viewMode !== 'hourly' && viewMode !== 'daily') {
    return (
      <View style={styles.infoCard}>
        <Text variant="caption" color={colors.info[700]}>
          El gráfico de variabilidad solo está disponible en vistas horaria y diaria.
        </Text>
      </View>
    );
  }

  const validPoints = useMemo(() => {
    if (!selectedData?.variability) return [];
    return selectedData.variability.filter(
      (p) =>
        p &&
        typeof p.q1 === 'number' &&
        typeof p.median === 'number' &&
        typeof p.q3 === 'number' &&
        typeof p.min === 'number' &&
        typeof p.max === 'number',
    );
  }, [selectedData]);

  if (!selectedData || validPoints.length === 0) {
    return (
      <View style={styles.infoCard}>
        <Text variant="caption" color={colors.warning[700]}>
          No hay datos de variabilidad para esta variable.
        </Text>
      </View>
    );
  }

  const globalMin = Math.min(...validPoints.map((p) => p.min));
  const globalMax = Math.max(...validPoints.map((p) => p.max));
  const barColor = getVariableColor(selectedData.variableName);
  const CHART_HEIGHT = 180;

  return (
    <View style={styles.container}>
      <View style={styles.headerRow}>
        <Text variant="label" color={semanticColors.textPrimary} style={styles.heading}>
          {viewMode === 'hourly' ? 'Variabilidad Horaria' : 'Variabilidad Diaria'}
        </Text>
        <View style={styles.selectorWrapper}>
          <Select
            options={variableOptions}
            value={selectedCode}
            onValueChange={setSelectedCode}
            style={styles.selector}
            accessibilityLabel="Variable"
          />
        </View>
      </View>

      {/* Y axis labels + boxplots */}
      <View style={styles.chartWrapper}>
        {/* Y axis */}
        <View style={[styles.yAxis, { height: CHART_HEIGHT }]}>
          <Text variant="caption" color={semanticColors.textTertiary} style={styles.yLabel}>
            {globalMax.toFixed(1)}
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary} style={styles.yLabel}>
            {((globalMax + globalMin) / 2).toFixed(1)}
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary} style={styles.yLabel}>
            {globalMin.toFixed(1)}
          </Text>
        </View>

        {/* Scrollable boxplots */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.boxplotRow}
        >
          {validPoints.map((point, i) => (
            <BoxplotItem
              key={point.timestamp + i}
              point={point}
              globalMin={globalMin}
              globalMax={globalMax}
              barColor={barColor}
              label={formatTimestamp(point.timestamp, viewMode)}
              chartHeight={CHART_HEIGHT}
            />
          ))}
        </ScrollView>
      </View>

      {/* Info footer */}
      <View style={styles.footer}>
        <Text variant="caption" color={semanticColors.textTertiary} style={styles.footerText}>
          La caja muestra el rango intercuartílico (IQR), la línea central es la mediana,
          y los bigotes los valores mínimo y máximo.
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
    gap: spacing.sm,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  heading: {
    fontWeight: typography.fontWeight.semibold,
    flex: 1,
  },
  selectorWrapper: {
    width: 140,
  },
  selector: {
    minHeight: 36,
  },
  chartWrapper: {
    flexDirection: 'row',
  },
  yAxis: {
    width: 40,
    justifyContent: 'space-between',
    alignItems: 'flex-end',
    paddingRight: 4,
  },
  yLabel: {
    fontSize: typography.fontSize.micro,
  },
  boxplotRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.sm,
    paddingLeft: spacing.xs,
  },
  boxplotItemContainer: {
    alignItems: 'center',
    width: 36,
  },
  boxplotCanvas: {
    width: 36,
    position: 'relative',
  },
  whisker: {
    position: 'absolute',
    width: 2,
    left: 17,
  },
  whiskerCap: {
    position: 'absolute',
    width: 14,
    height: 2,
    left: 11,
  },
  box: {
    position: 'absolute',
    width: 24,
    left: 6,
    borderWidth: 1.5,
    borderRadius: borderRadius.xs,
  },
  medianLine: {
    position: 'absolute',
    width: 24,
    height: 2,
    left: 6,
  },
  boxplotLabel: {
    fontSize: typography.fontSize.micro,
    marginTop: 4,
    textAlign: 'center',
  },
  infoCard: {
    backgroundColor: colors.info[50],
    borderLeftWidth: 3,
    borderLeftColor: colors.info[400],
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginHorizontal: spacing.md,
  },
  footer: {
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
    padding: spacing.sm,
  },
  footerText: {
    lineHeight: 16,
  },
});
