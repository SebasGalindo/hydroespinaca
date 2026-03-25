import React from 'react';
import { ScrollView, View, StyleSheet, ViewStyle } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { ForecastDayCard } from '../molecules/ForecastDayCard';
import { spacing, semanticColors } from '@hydroespinaca/shared';
import type { DailyForecast } from '@hydroespinaca/shared';

export interface ForecastCarouselProps {
  days: DailyForecast[];
  loading?: boolean;
  error?: string | null;
  style?: ViewStyle;
  testID?: string;
}

export const ForecastCarousel = React.memo(function ForecastCarousel({
  days,
  loading = false,
  error,
  style,
  testID,
}: ForecastCarouselProps): React.ReactElement {
  if (loading) {
    return (
      <View style={[styles.center, style]} testID={testID}>
        <Spinner size="sm" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={[styles.center, style]} testID={testID}>
        <Text variant="caption" color={semanticColors.error}>{error}</Text>
      </View>
    );
  }

  if (days.length === 0) {
    return (
      <View style={[styles.center, style]} testID={testID}>
        <Text variant="caption" color={semanticColors.textSecondary}>
          Sin datos de pronóstico
        </Text>
      </View>
    );
  }

  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.scroll}
      style={style}
      testID={testID}
    >
      {days.map((day, idx) => (
        <ForecastDayCard
          key={day.dateTime}
          forecast={day}
          isToday={idx === 0}
          style={idx < days.length - 1 ? styles.cardSpacing : undefined}
          testID={`${testID}-day-${idx}`}
        />
      ))}
    </ScrollView>
  );
});

const styles = StyleSheet.create({
  scroll: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
  cardSpacing: {
    marginRight: spacing.sm,
  },
  center: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xl,
  },
});
