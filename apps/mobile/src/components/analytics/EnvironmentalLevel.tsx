import React from 'react';
import { View, StyleSheet } from 'react-native';
import { colors, spacing, borderRadius } from '@hydroespinaca/shared';
import type { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Alert } from '../molecules/Alert';
import { SummaryCards } from './SummaryCards';
import { TimelineChart } from './TimelineChart';
import { BoxplotChart } from './BoxplotChart';
import type { ViewMode } from '../../utils/analyticsFilters';
import { SkeletonLoader } from '../organisms/SkeletonLoader';

interface EnvironmentalLevelProps {
  variables: EnvironmentalVariableAggregate[];
  viewMode: ViewMode;
  isLoading: boolean;
  error: string | null;
}

export function EnvironmentalLevel({
  variables,
  viewMode,
  isLoading,
  error,
}: EnvironmentalLevelProps): React.ReactElement {
  if (isLoading) {
    return (
      <View style={styles.container}>
        <SkeletonLoader height={120} style={styles.skeleton} />
        <SkeletonLoader height={260} style={styles.skeleton} />
        <SkeletonLoader height={220} style={styles.skeleton} />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.container}>
        <Alert
          type="error"
          title="Error al cargar datos"
          message={error}
          style={styles.alert}
        />
      </View>
    );
  }

  if (variables.length === 0) {
    return (
      <View style={styles.container}>
        <Alert
          type="warning"
          message="No hay datos disponibles para el rango seleccionado. Intenta con otro rango de fechas."
          style={styles.alert}
        />
      </View>
    );
  }

  const hasVariability = variables.some(
    (v) => v.variability && v.variability.length > 0,
  );

  return (
    <View style={styles.container}>
      <SummaryCards variables={variables} />
      <TimelineChart variables={variables} />
      {(viewMode === 'hourly' || viewMode === 'daily') && hasVariability && (
        <BoxplotChart variables={variables} viewMode={viewMode} />
      )}
      {(viewMode === 'hourly' || viewMode === 'daily') && !hasVariability && (
        <Alert
          type="info"
          message="El gráfico de variabilidad solo está disponible cuando hay suficientes datos."
          style={styles.alert}
        />
      )}
      {viewMode !== 'hourly' && viewMode !== 'daily' && (
        <Alert
          type="info"
          message="El gráfico de variabilidad solo está disponible en las vistas horaria y diaria."
          style={styles.alert}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
    paddingBottom: spacing.md,
  },
  skeleton: {
    marginHorizontal: spacing.md,
    borderRadius: borderRadius.lg,
  },
  alert: {
    marginHorizontal: spacing.md,
  },
});
