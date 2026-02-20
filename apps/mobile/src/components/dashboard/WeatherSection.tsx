import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { WeatherCard } from './WeatherCard';
import { semanticColors, spacing, typography } from '@hydroespinaca/shared';
import type { WeatherSummary } from '@hydroespinaca/shared';

export interface WeatherSectionProps {
  weather: WeatherSummary | null;
  isLoading: boolean;
  error: string | null;
}

export function WeatherSection({
  weather,
  isLoading,
  error,
}: WeatherSectionProps): React.ReactElement {
  return (
    <View style={styles.section}>
      <Text variant="h2" color={semanticColors.primary} style={styles.sectionTitle}>
        Condiciones Climáticas
      </Text>
      <WeatherCard
        weather={weather}
        isLoading={isLoading}
        error={error}
      />
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
});
