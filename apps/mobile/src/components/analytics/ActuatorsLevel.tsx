import React, { useState, useEffect, useCallback } from 'react';
import { View, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Alert } from '../molecules/Alert';
import { SkeletonLoader } from '../organisms/SkeletonLoader';
import { DurationChart } from './DurationChart';
import { ProportionChart } from './ProportionChart';
import type { ActuatorActivity } from '../../utils/actuatorMapper';

interface ActuatorsLevelProps {
  data: ActuatorActivity[];
  isLoading: boolean;
  error: string | null;
  isSingleDay: boolean;
}

type ActuatorViewMode = 'all' | 'duration' | 'proportion';

export function ActuatorsLevel({
  data,
  isLoading,
  error,
  isSingleDay,
}: ActuatorsLevelProps): React.ReactElement {
  const [viewMode, setViewMode] = useState<ActuatorViewMode>('all');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  useEffect(() => {
    if (data.length > 0) {
      setSelectedIds(data.map((a) => a.actuatorId));
    }
  }, [data]);

  const toggleActuator = useCallback((id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
  }, []);

  if (isLoading) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={280} style={styles.skeleton} />
        <SkeletonLoader height={280} style={styles.skeleton} />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.container}>
        <Alert type="error" title="Error" message={error} style={styles.alert} />
      </View>
    );
  }

  const filteredData = data.filter((a) => selectedIds.includes(a.actuatorId));

  const viewModes: { value: ActuatorViewMode; label: string }[] = [
    { value: 'all', label: 'Todos' },
    { value: 'duration', label: 'Duración' },
    { value: 'proportion', label: 'Proporción' },
  ];

  return (
    <View style={styles.container}>
      {/* Controls card */}
      <View style={styles.controls}>
        {/* View mode */}
        <View style={styles.sectionLabel}>
          <Text variant="caption" color={semanticColors.textSecondary} style={styles.sectionLabelText}>
            Vista
          </Text>
        </View>
        <View style={styles.chipsRow}>
          {viewModes.map((m) => (
            <TouchableOpacity
              key={m.value}
              style={[styles.chip, viewMode === m.value && styles.chipActive]}
              onPress={() => setViewMode(m.value)}
            >
              <Text
                variant="caption"
                color={viewMode === m.value ? colors.white : semanticColors.textSecondary}
                style={styles.chipLabel}
              >
                {m.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* Actuator toggles */}
        {data.length > 0 && (
          <>
            <View style={styles.sectionLabel}>
              <Text variant="caption" color={semanticColors.textSecondary} style={styles.sectionLabelText}>
                Actuadores
              </Text>
            </View>
            <ScrollView
              horizontal
              showsHorizontalScrollIndicator={false}
              contentContainerStyle={styles.chipsRow}
            >
              {data.map((a) => {
                const isActive = selectedIds.includes(a.actuatorId);
                return (
                  <TouchableOpacity
                    key={a.actuatorId}
                    style={[styles.chip, isActive && styles.chipActive]}
                    onPress={() => toggleActuator(a.actuatorId)}
                  >
                    <Text
                      variant="caption"
                      color={isActive ? colors.white : semanticColors.textSecondary}
                      style={styles.chipLabel}
                    >
                      {a.actuatorName}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </>
        )}
      </View>

      {/* Charts */}
      {filteredData.length === 0 ? (
        <Alert
          type="warning"
          message="Selecciona al menos un actuador para ver los gráficos."
          style={styles.alert}
        />
      ) : (
        <>
          {(viewMode === 'all' || viewMode === 'duration') && (
            <DurationChart data={filteredData} />
          )}
          {(viewMode === 'all' || viewMode === 'proportion') && (
            <ProportionChart data={filteredData} />
          )}
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
    paddingBottom: spacing.md,
  },
  controls: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    padding: spacing.md,
    marginHorizontal: spacing.md,
    gap: spacing.sm,
  },
  sectionLabel: {
    marginTop: spacing.xs,
  },
  sectionLabelText: {
    fontWeight: typography.fontWeight.medium,
  },
  chipsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  chip: {
    paddingVertical: 6,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.full,
    backgroundColor: colors.gray[100],
  },
  chipActive: {
    backgroundColor: colors.hidro[600],
  },
  chipLabel: {
    fontSize: typography.fontSize.xxs,
  },
  skeleton: {
    marginHorizontal: spacing.md,
    borderRadius: borderRadius.lg,
  },
  alert: {
    marginHorizontal: spacing.md,
  },
});
