import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { ForecastCarousel } from '../organisms/ForecastCarousel';
import { spacing, semanticColors, typography } from '@hydroespinaca/shared';
import type { DailyForecast } from '@hydroespinaca/shared';

export interface ForecastSectionProps {
  days: DailyForecast[];
  loading?: boolean;
  error?: string | null;
}

export function ForecastSection({
  days,
  loading,
  error,
}: ForecastSectionProps): React.ReactElement {
  return (
    <View style={styles.section}>
      <Text variant="h2" color={semanticColors.primary} style={styles.title}>
        Pronóstico 8 Días
      </Text>
      <ForecastCarousel
        days={days}
        loading={loading}
        error={error}
        testID="forecast-carousel"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    marginBottom: spacing.lg,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.sm,
  },
});
