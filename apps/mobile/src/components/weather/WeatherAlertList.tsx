import React, { useMemo, useState } from 'react';
import { View, ScrollView, Pressable, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Spinner } from '../atoms/Spinner';
import { WeatherAlertItem } from '../molecules/WeatherAlertItem';
import {
  colors,
  spacing,
  semanticColors,
  typography,
  borderRadius,
  ALERT_TYPE_LABELS,
} from '@hydroespinaca/shared';
import type { WeatherAlert, AlertType } from '@hydroespinaca/shared';

export interface WeatherAlertListProps {
  alerts: WeatherAlert[];
  userId: string;
  loading?: boolean;
  error?: string | null;
  onPress: (alert: WeatherAlert) => void;
  onMarkRead: (alertId: string) => void;
}

type FilterValue = 'all' | AlertType;

export function WeatherAlertList({
  alerts,
  userId,
  loading,
  error,
  onPress,
  onMarkRead,
}: WeatherAlertListProps): React.ReactElement {
  const [filterType, setFilterType] = useState<FilterValue>('all');

  // Show only the alert types that actually appear in the current data,
  // so the chip row doesn't crowd with irrelevant options.
  const availableTypes = useMemo<AlertType[]>(() => {
    const set = new Set<AlertType>();
    alerts.forEach((a) => set.add(a.alertType as AlertType));
    return Array.from(set);
  }, [alerts]);

  const filteredAlerts = useMemo(
    () =>
      filterType === 'all'
        ? alerts
        : alerts.filter((a) => a.alertType === filterType),
    [alerts, filterType],
  );

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

      {/* Filtros por tipo */}
      {alerts.length > 0 && availableTypes.length > 0 && (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.chipsRow}
          style={styles.chipsScroll}
        >
          <FilterChip
            label="Todas"
            count={alerts.length}
            active={filterType === 'all'}
            onPress={() => setFilterType('all')}
          />
          {availableTypes.map((type) => {
            const count = alerts.filter((a) => a.alertType === type).length;
            return (
              <FilterChip
                key={type}
                label={ALERT_TYPE_LABELS[type] ?? type}
                count={count}
                active={filterType === type}
                onPress={() => setFilterType(type)}
              />
            );
          })}
        </ScrollView>
      )}

      {filteredAlerts.length === 0 ? (
        <Text variant="body" color={semanticColors.textSecondary} style={styles.empty}>
          {alerts.length === 0
            ? 'No hay alertas meteorológicas recientes.'
            : 'No hay alertas de este tipo.'}
        </Text>
      ) : (
        filteredAlerts.map(alert => (
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

interface FilterChipProps {
  label: string;
  count: number;
  active: boolean;
  onPress: () => void;
}

function FilterChip({ label, count, active, onPress }: FilterChipProps): React.ReactElement {
  return (
    <Pressable
      onPress={onPress}
      style={[styles.chip, active && styles.chipActive]}
      accessibilityRole="button"
      accessibilityState={{ selected: active }}
    >
      <Text
        variant="caption"
        color={active ? colors.white : semanticColors.textSecondary}
        style={active ? styles.chipLabelActive : undefined}
      >
        {label}
      </Text>
      <View style={[styles.chipCount, active && styles.chipCountActive]}>
        <Text
          variant="caption"
          color={active ? colors.white : semanticColors.textTertiary}
          style={styles.chipCountText}
        >
          {count}
        </Text>
      </View>
    </Pressable>
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
  chipsScroll: {
    marginBottom: spacing.sm,
  },
  chipsRow: {
    gap: spacing.sm,
    paddingVertical: spacing.xs,
  },
  chip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.md,
    borderRadius: 999,
    backgroundColor: colors.white,
    borderWidth: 1,
    borderColor: colors.gray[200],
  },
  chipActive: {
    backgroundColor: semanticColors.primary,
    borderColor: semanticColors.primary,
  },
  chipLabelActive: {
    fontWeight: typography.fontWeight.semibold,
  },
  chipCount: {
    minWidth: 20,
    paddingHorizontal: 6,
    paddingVertical: 1,
    borderRadius: borderRadius.sm,
    backgroundColor: colors.gray[100],
    alignItems: 'center',
  },
  chipCountActive: {
    backgroundColor: 'rgba(255, 255, 255, 0.25)',
  },
  chipCountText: {
    fontSize: 10,
    fontWeight: typography.fontWeight.semibold,
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
