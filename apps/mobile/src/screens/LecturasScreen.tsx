import React, { useEffect, useMemo, useState } from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, colors } from '@hidroespinaca/shared';
import { Text } from '../components/atoms';
import { Card } from '../components/molecules';
import { ReadingsTable, FiltersPanel } from '../components/organisms';
import { ScreenLayout } from '../components/templates/ScreenLayout';
import { useReadingsStore } from '@hidroespinaca/shared';
import type { FilterValues } from '../components/organisms/FiltersPanel';
import type { OperatorType } from '../components/atoms/OperatorChips';
import type { TimeValue } from '../components/molecules/TimePicker';

// Funciones de utilidad para formateo de fechas
function formatDateYMD(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function formatTimeHM(date: Date): string {
  const h = String(date.getHours()).padStart(2, '0');
  const m = String(date.getMinutes()).padStart(2, '0');
  return `${h}:${m}`;
}

function formatTimeValueHM(timeValue: TimeValue): string {
  const h = String(timeValue.hours).padStart(2, '0');
  const m = String(timeValue.minutes).padStart(2, '0');
  return `${h}:${m}`;
}

export function LecturasScreen(): React.ReactElement {
  const { sensorSummary, individualReadings, initializeReadings } = useReadingsStore();

  // Estado de filtros usando el tipo del FiltersPanel
  const [filters, setFilters] = useState<FilterValues>({
    sensor: 'Todos',
    numericValue: '',
    operator: 'eq' as OperatorType,
  });

  useEffect(() => {
    // Inicializar lecturas de ejemplo si aún no se han cargado
    if (!sensorSummary.length || !individualReadings.length) {
      initializeReadings();
    }
  }, [initializeReadings, sensorSummary.length, individualReadings.length]);

  // Opciones de sensores para el FiltersPanel
  const sensorOptions = useMemo(() => {
    const base = [{ label: 'Todos', value: 'Todos' }];
    const dynamic = sensorSummary.map(s => ({ label: s.sensor, value: s.sensor }));
    return [...base, ...dynamic];
  }, [sensorSummary]);

  // Filtrar lecturas individuales según los filtros seleccionados
  const filteredReadings = useMemo(() => {
    let data = individualReadings;

    if (filters.date) {
      const ymd = formatDateYMD(filters.date);
      data = data.filter(r => r.fecha.startsWith(ymd));
    }

    if (filters.time) {
      const hm = formatTimeValueHM(filters.time);
      data = data.filter(r => r.fecha.includes(hm));
    }

    if (filters.sensor && filters.sensor !== 'Todos') {
      data = data.filter(r => r.sensor === filters.sensor);
    }

    if (filters.numericValue.trim() !== '') {
      const num = Number(filters.numericValue);
      if (!Number.isNaN(num)) {
        data = data.filter(r => {
          switch (filters.operator) {
            case 'gt': return r.valor > num;
            case 'gte': return r.valor >= num;
            case 'lt': return r.valor < num;
            case 'lte': return r.valor <= num;
            default: return r.valor === num;
          }
        });
      }
    }

    return data;
  }, [individualReadings, filters]);

  // Componente de header que incluye toda la información estática
  const HeaderComponent = () => (
    <View style={styles.headerContainer}>
      {/* 1. Encabezado de página */}
      <Card style={styles.headerCard}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Lecturas de Sensores
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
          Monitoreo en tiempo real de las variables del cultivo
        </Text>
      </Card>

      {/* 2. Resumen de Sensores */}
      <Card style={styles.transparentCard}>
        <Text variant="h3" style={styles.sectionTitle}>
          Resumen de los últimos 10 minutos
        </Text>
        <ReadingsTable
          mode="summary"
          summaryData={sensorSummary}
          loading={false}
          error={null}
          showSensorIcons
          dateFormat="short"
          estimatedItemSize={200}
          title=""
        />
      </Card>

      {/* 3. Lecturas individuales */}
      <Text variant="h3" style={styles.sectionTitle}>
        Lecturas individuales
      </Text>

      {/* 4. Filtros */}
      <Card style={styles.transparentCard}>
        <FiltersPanel
          values={filters}
          onFiltersChange={setFilters}
          sensorOptions={sensorOptions}
          showClearButton={true}
          testID="lecturas-filters"
        />
      </Card>
    </View>
  );

  return (
    <ScreenLayout>
      <View style={styles.container}>
        <ReadingsTable
          mode="individual"
          individualData={filteredReadings}
          loading={false}
          error={null}
          estimatedItemSize={120}
          ListHeaderComponent={HeaderComponent}
          title=""
          contentContainerStyle={styles.readingsContainer}
        />
      </View>
    </ScreenLayout>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  headerContainer: {
    paddingHorizontal: spacing.md,
    paddingTop: spacing.lg,
    paddingBottom: spacing.lg,
    gap: spacing.lg,
    backgroundColor: colors.hidro.bgLight,
  },
  headerCard: {
    alignItems: 'center',
    padding: spacing.lg,
    backgroundColor: colors.hidro.bgLight,
    borderWidth: 0,
    shadowOpacity: 0,
    elevation: 0,
  },
  title: {
    textAlign: 'center',
    marginBottom: spacing.sm,
  },
  subtitle: {
    textAlign: 'center',
    lineHeight: 24,
  },

  sectionTitle: {
    color: semanticColors.textPrimary,
    marginBottom: spacing.sm,
  },
  transparentCard: {
    backgroundColor: 'transparent',
    borderWidth: 0,
    shadowOpacity: 0,
    elevation: 0,
    paddingVertical: spacing.md,
    paddingHorizontal: 0,
  },
  readingsContainer: {
    paddingHorizontal: spacing.md, // Mismo padding que headerContainer para alineación
  },
});