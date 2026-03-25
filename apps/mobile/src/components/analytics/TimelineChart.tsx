import React, { useState, useMemo, useCallback } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Dimensions,
} from 'react-native';
import { LineChart } from 'react-native-gifted-charts';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import type { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { getVariableColor } from '../../utils/actuatorMapper';

interface TimelineChartProps {
  variables: EnvironmentalVariableAggregate[];
}

function formatTimestamp(ts: string): string {
  const d = new Date(ts);
  const h = String(d.getHours()).padStart(2, '0');
  const m = String(d.getMinutes()).padStart(2, '0');
  const day = d.getDate();
  const mon = d.toLocaleString('es-CO', { month: 'short' });
  return `${day} ${mon}\n${h}:${m}`;
}

const SCREEN_WIDTH = Dimensions.get('window').width;

export function TimelineChart({ variables }: TimelineChartProps): React.ReactElement | null {
  const [selectedVars, setSelectedVars] = useState<string[]>(
    variables.map((v) => v.variableName),
  );

  const toggleVariable = useCallback((name: string) => {
    setSelectedVars((prev) =>
      prev.includes(name)
        ? prev.filter((v) => v !== name)
        : [...prev, name],
    );
  }, []);

  // Build datasets — one line per variable
  const { dataSets, xLabels, yMin, yMax } = useMemo(() => {
    const filtered = variables.filter((v) => selectedVars.includes(v.variableName));
    if (filtered.length === 0) {
      return { dataSets: [], xLabels: [] as string[], yMin: 0, yMax: 100 };
    }

    // Use the first variable's trend timestamps as reference for labels
    const refTrend = filtered[0]!.trend;
    const labels = refTrend.map((p) => formatTimestamp(p.timestamp));

    // Compute global min/max for Y axis
    let globalMin = Infinity;
    let globalMax = -Infinity;
    filtered.forEach((v) => {
      v.trend.forEach((p) => {
        if (p.avg < globalMin) globalMin = p.avg;
        if (p.avg > globalMax) globalMax = p.avg;
      });
    });
    const padding = (globalMax - globalMin) * 0.1 || 1;

    // Build datasets
    const sets = filtered.map((variable) => {
      const color = getVariableColor(variable.variableName);
      return {
        data: variable.trend.map((p) => ({ value: p.avg })),
        color,
        name: variable.variableName,
      };
    });

    return {
      dataSets: sets,
      xLabels: labels,
      yMin: Math.floor(globalMin - padding),
      yMax: Math.ceil(globalMax + padding),
    };
  }, [variables, selectedVars]);

  if (variables.length === 0) return null;

  // When we have multiple datasets, gifted-charts expects the first as `data`
  // and rest as `dataSet` entries. However, for simplicity with multi-line
  // we render one LineChart per variable overlaid if only one is selected,
  // or show a single line if only one selected.
  // For best UX, show one variable at a time in the chart with toggle.

  const activeDataset = dataSets.length > 0 ? dataSets[0] : null;

  // Determine spacing between points based on data count
  const pointCount = activeDataset?.data.length || 1;
  const chartSpacing = Math.max(40, Math.floor((SCREEN_WIDTH - 100) / Math.min(pointCount, 12)));

  // Label step: show every Nth label to prevent overlap
  const labelStep = pointCount > 12 ? Math.ceil(pointCount / 8) : 1;

  return (
    <View style={styles.container}>
      <Text variant="label" color={semanticColors.textPrimary} style={styles.heading}>
        Tendencias Ambientales
      </Text>

      {/* Variable toggle chips */}
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.chipRow}
      >
        {variables.map((v) => {
          const isActive = selectedVars.includes(v.variableName);
          const varColor = getVariableColor(v.variableName);
          return (
            <TouchableOpacity
              key={v.variableCode}
              style={[
                styles.chip,
                isActive && { backgroundColor: varColor },
              ]}
              onPress={() => toggleVariable(v.variableName)}
            >
              <Text
                variant="caption"
                color={isActive ? colors.white : semanticColors.textSecondary}
                style={styles.chipLabel}
              >
                {v.variableName}
              </Text>
            </TouchableOpacity>
          );
        })}
      </ScrollView>

      {/* Chart */}
      {activeDataset ? (
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          <LineChart
            data={activeDataset.data}
            dataSet={dataSets.slice(1).map((ds) => ({
              data: ds.data,
              color: ds.color,
              dataPointsColor: ds.color,
            }))}
            width={Math.max(SCREEN_WIDTH - 80, pointCount * chartSpacing)}
            height={220}
            spacing={chartSpacing}
            color={activeDataset.color}
            dataPointsColor={activeDataset.color}
            thickness={2}
            dataPointsRadius={3}
            yAxisColor={colors.gray[200]}
            xAxisColor={colors.gray[200]}
            yAxisTextStyle={styles.axisText}
            xAxisLabelTexts={xLabels.map((l, i) =>
              i % labelStep === 0 ? l : '',
            )}
            xAxisLabelTextStyle={styles.xAxisLabel}
            noOfSections={5}
            curved
            startFillColor={`${activeDataset.color}20`}
            endFillColor={`${activeDataset.color}05`}
            areaChart
            hideRules={false}
            rulesColor={colors.gray[100]}
            isAnimated
            animationDuration={600}
            pointerConfig={{
              pointerStripHeight: 200,
              pointerStripColor: colors.gray[300],
              pointerStripWidth: 1,
              pointerColor: activeDataset.color,
              radius: 5,
              pointerLabelWidth: 100,
              pointerLabelHeight: 60,
              pointerLabelComponent: (items: { value: number }[]) => {
                if (!items || items.length === 0) return null;
                return (
                  <View style={styles.tooltip}>
                    <Text variant="caption" color={colors.white} style={styles.tooltipText}>
                      {items[0]!.value.toFixed(2)}
                    </Text>
                  </View>
                );
              },
            }}
          />
        </ScrollView>
      ) : (
        <View style={styles.emptyChart}>
          <Text variant="caption" color={semanticColors.textTertiary}>
            Selecciona al menos una variable
          </Text>
        </View>
      )}

      {/* Legend */}
      {dataSets.length > 1 && (
        <View style={styles.legend}>
          {dataSets.map((ds) => (
            <View key={ds.name} style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: ds.color }]} />
              <Text variant="caption" color={semanticColors.textSecondary}>
                {ds.name}
              </Text>
            </View>
          ))}
        </View>
      )}
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
  chipRow: {
    flexDirection: 'row',
    gap: spacing.xs,
    paddingVertical: spacing.xs,
  },
  chip: {
    paddingVertical: 4,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.full,
    backgroundColor: colors.gray[100],
  },
  chipLabel: {
    fontSize: typography.fontSize.xxs,
  },
  axisText: {
    fontSize: typography.fontSize.xxs,
    color: colors.gray[500],
  },
  xAxisLabel: {
    fontSize: typography.fontSize.micro,
    color: colors.gray[500],
    width: 50,
    textAlign: 'center',
  },
  tooltip: {
    backgroundColor: semanticColors.overlayStrong,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: borderRadius.sm,
  },
  tooltipText: {
    fontWeight: typography.fontWeight.semibold,
    fontSize: typography.fontSize.xs,
  },
  emptyChart: {
    height: 180,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.gray[50],
    borderRadius: borderRadius.md,
  },
  legend: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    paddingTop: spacing.xs,
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
