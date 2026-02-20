import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { VariableCard } from './VariableCard';
import { VariableCardSkeleton } from './VariableCardSkeleton';
import type { VariableStatus, TrendDirection } from './VariableCard';
import {
  semanticColors,
  spacing,
  typography,
  formatNumericValue,
  calculateVariableStatus as sharedCalculateVariableStatus,
  calculateTrend as sharedCalculateTrend,
  getAlertConfig as sharedGetAlertConfig,
  type ReadingItem,
  type IconType,
} from '@hydroespinaca/shared';

export interface VariablesGridProps {
  readings: ReadingItem[];
  previousReadings: ReadingItem[];
  isArtificialLightActive: boolean;
  isLoading?: boolean;
}

const getIconType = (name: string): IconType => {
  const lowerName = name.toLowerCase();

  if (lowerName.includes('temperatura') && lowerName.includes('agua')) return 'water';
  if (lowerName.includes('temperatura')) return 'temperature';
  if (lowerName.includes('humedad')) return 'humidity';
  if (lowerName.includes('luz') || lowerName.includes('luminosidad')) return 'sun';
  if (lowerName.includes('conductividad') || lowerName.includes('ec')) return 'electric';
  if (lowerName.includes('nivel')) return 'ruler';
  if (lowerName.includes('ph')) return 'ph';

  return 'temperature';
};

export function VariablesGrid({
  readings,
  previousReadings,
  isArtificialLightActive,
  isLoading = false,
}: VariablesGridProps): React.ReactElement {
  const getPreviousValue = (variableName: string): number | null => {
    const previous = previousReadings.find(
      r => r.name.toLowerCase() === variableName.toLowerCase()
    );
    return previous?.value || null;
  };

  return (
    <View style={styles.section}>
      <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
        Variables del Sistema
      </Text>
      {isLoading ? (
        <VariableCardSkeleton count={6} />
      ) : (
      <View style={styles.grid}>
        {readings.map((reading, index) => {
          const alertConfig = sharedGetAlertConfig(reading.name);
          const status = sharedCalculateVariableStatus(reading, alertConfig);
          const previousValue = getPreviousValue(reading.name);
          const trend = sharedCalculateTrend(reading.value, previousValue);

          return (
            <View key={reading.name} style={styles.cardWrapper}>
              <VariableCard
                title={reading.name}
                value={`${formatNumericValue(reading.value)} ${reading.unit}`}
                optimal={`Óptima: ${formatNumericValue(reading.optimalMin)} – ${formatNumericValue(reading.optimalMax)} ${reading.unit}`}
                iconType={getIconType(reading.name)}
                status={status}
                trend={trend}
                artificialLightActive={isArtificialLightActive}
                animationIndex={index}
              />
            </View>
          );
        })}
      </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.md,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  cardWrapper: {
    width: '48%',
  },
});
