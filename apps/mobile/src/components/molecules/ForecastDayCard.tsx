import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { WeatherIcon } from '../atoms/WeatherIcon';
import { Card } from './Card';
import {
  semanticColors, spacing, colors,
  type DailyForecast,
} from '@hydroespinaca/shared';

export interface ForecastDayCardProps {
  forecast: DailyForecast;
  isToday?: boolean;
  onPress?: () => void;
  style?: ViewStyle;
  testID?: string;
}

function getDayLabel(dateTime: string, isToday: boolean): string {
  if (isToday) return 'Hoy';
  const date = new Date(dateTime);
  const days = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
  return days[date.getDay()] ?? '';
}

function getPopColor(pop: number): string {
  if (pop >= 0.7) return colors.info[600];
  if (pop >= 0.4) return colors.info[400];
  return colors.info[300];
}

function getUviColor(uvi: number): string {
  if (uvi >= 8) return colors.error[500];
  if (uvi >= 6) return colors.warning[500];
  if (uvi >= 3) return colors.warning[400];
  return colors.success[500];
}

export const ForecastDayCard = React.memo(function ForecastDayCard({
  forecast,
  isToday = false,
  onPress,
  style,
  testID,
}: ForecastDayCardProps): React.ReactElement {
  const dayLabel = getDayLabel(forecast.dateTime, isToday);
  const dateStr = new Date(forecast.dateTime).toLocaleDateString('es-CO', { day: 'numeric', month: 'short' });

  return (
    <Card
      variant={isToday ? 'filled' : 'elevated'}
      padding="sm"
      style={style ? { ...styles.card, ...style } : styles.card}
      testID={testID}
    >
      <View style={styles.header}>
        <Text variant="label" weight="bold" color={isToday ? semanticColors.primary : semanticColors.textPrimary}>
          {dayLabel}
        </Text>
        <Text variant="caption" color={semanticColors.textSecondary}>{dateStr}</Text>
      </View>

      <WeatherIcon icon={forecast.icon} size={40} style={styles.icon} />

      <View style={styles.temps}>
        <Text variant="body" weight="bold" color={semanticColors.textPrimary}>
          {Math.round(forecast.tempMax)}°
        </Text>
        <Text variant="caption" color={semanticColors.textSecondary}>
          {Math.round(forecast.tempMin)}°
        </Text>
      </View>

      <Text variant="caption" color={semanticColors.textSecondary} numberOfLines={1} style={styles.condition}>
        {forecast.description}
      </Text>

      {/* Precipitation probability */}
      <View style={styles.dataRow}>
        <Text variant="caption" color={getPopColor(forecast.pop)}>
          💧 {Math.round(forecast.pop * 100)}%
        </Text>
      </View>

      {/* UV */}
      <View style={styles.dataRow}>
        <Text variant="caption" color={getUviColor(forecast.uvi)}>
          ☀️ UV {Math.round(forecast.uvi)}
        </Text>
      </View>

      {/* Wind */}
      <View style={styles.dataRow}>
        <Text variant="caption" color={semanticColors.textSecondary}>
          💨 {forecast.windSpeed.toFixed(1)} m/s
        </Text>
      </View>
    </Card>
  );
});

const styles = StyleSheet.create({
  card: {
    width: 110,
    alignItems: 'center',
    marginRight: spacing.sm,
  },
  header: {
    alignItems: 'center',
    marginBottom: spacing.xs,
  },
  icon: {
    marginVertical: spacing.xs,
  },
  temps: {
    flexDirection: 'row',
    gap: spacing.xs,
    alignItems: 'baseline',
  },
  condition: {
    textAlign: 'center',
    marginTop: spacing.xs,
  },
  dataRow: {
    marginTop: 2,
  },
});
