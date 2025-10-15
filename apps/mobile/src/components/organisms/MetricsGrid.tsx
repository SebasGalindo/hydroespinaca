import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { VariableCard } from '../molecules';
import { spacing } from '@hydroespinaca/shared';
import type { MetricData } from '@hydroespinaca/shared';

interface MetricsGridProps {
  metrics: MetricData[];
  numColumns?: number;
  showOptimalRanges?: boolean;
  onVariablePress?: (metric: MetricData) => void;
}

const OPTIMAL_RANGES = {
  'Temperatura': '23-25°C',
  'Humedad': '70-80 %',
  'pH': '6.0-7.0',
  'Luz Solar': '80-90%',
  'Conductividad': '1.0-1.5 mS/cm'
};

export function MetricsGrid({ 
  metrics, 
  numColumns = 1,
  showOptimalRanges = true,
  onVariablePress 
}: MetricsGridProps): JSX.Element {
  const getOptimalRange = (title: string): string | undefined => {
    if (!showOptimalRanges) return undefined;
    return OPTIMAL_RANGES[title as keyof typeof OPTIMAL_RANGES];
  };

  const renderMetricCard = (metric: MetricData, index: number) => {
    const optimalRange = getOptimalRange(metric.title);
    return (
      <View 
        key={metric.title} 
        style={[
          styles.cardContainer,
          numColumns > 1 && {
            width: `${(100 / numColumns) - 2}%`,
            marginRight: index % numColumns === numColumns - 1 ? 0 : spacing.sm
          }
        ]}
      >
        <VariableCard
          metric={metric}
          {...(optimalRange && { optimalRange })}
          onPress={() => onVariablePress?.(metric)}
        />
      </View>
    );
  };

  if (numColumns === 1) {
    return (
      <ScrollView 
        style={styles.container}
        showsVerticalScrollIndicator={false}
        contentContainerStyle={styles.scrollContent}
      >
        {metrics.map((metric, index) => renderMetricCard(metric, index))}
      </ScrollView>
    );
  }

  return (
    <ScrollView 
      style={styles.container}
      showsVerticalScrollIndicator={false}
      contentContainerStyle={styles.scrollContent}
    >
      <View style={styles.gridContainer}>
        {metrics.map((metric, index) => renderMetricCard(metric, index))}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  scrollContent: {
    padding: spacing.md,
    paddingBottom: spacing.xl,
  },
  gridContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-around'
  },
  cardContainer: {
    marginBottom: spacing.md,
  },
});