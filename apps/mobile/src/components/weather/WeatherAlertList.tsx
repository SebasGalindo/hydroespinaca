import React from 'react';
import { View, FlatList, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { WeatherAlertItem } from '../molecules/WeatherAlertItem';
import { spacing, semanticColors, typography } from '@hydroespinaca/shared';
import type { WeatherAlert } from '@hydroespinaca/shared';

export interface WeatherAlertListProps {
  alerts: WeatherAlert[];
  userId: string;
  loading?: boolean;
  error?: string | null;
  onPress: (alert: WeatherAlert) => void;
  onMarkRead: (alertId: string) => void;
}

export function WeatherAlertList({
  alerts,
  userId,
  loading,
  error,
  onPress,
  onMarkRead,
}: WeatherAlertListProps): React.ReactElement {
  if (loading && alerts.length === 0) {
    return (
      <View style={styles.center}>
        <Spinner size="sm" />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.section}>
        <Text variant="caption" color={semanticColors.error}>{error}</Text>
      </View>
    );
  }

  return (
    <View style={styles.section}>
      <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
        Alertas Recientes
      </Text>
      {alerts.length === 0 ? (
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          No hay alertas meteorológicas recientes.
        </Text>
      ) : (
        alerts.map(alert => (
          <WeatherAlertItem
            key={alert.id}
            alert={alert}
            userId={userId}
            onPress={() => onPress(alert)}
            onMarkRead={() => onMarkRead(alert.id)}
            testID={`weather-alert-${alert.id}`}
          />
        ))
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.sm,
  },
  center: {
    alignItems: 'center',
    paddingVertical: spacing.xl,
  },
  empty: {
    textAlign: 'center',
    marginVertical: spacing.md,
  },
});
