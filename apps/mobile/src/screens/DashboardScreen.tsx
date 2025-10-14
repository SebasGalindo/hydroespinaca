import React, { useState, useEffect } from 'react';
import { View, StyleSheet, SafeAreaView, ScrollView } from 'react-native';
import { semanticColors, spacing, colors, useDashboardStore, CHART_VARIABLES } from '@hidroespinaca/shared';
import { Text } from '../components/atoms/Text';
import {
  FilterTimeCard,
  TimeSeriesCard,
  ScatterPlotCard,
  CustomChartCard,
  ChartType,
} from '../components/organisms';
import { TimeFilters } from '../components/organisms/FilterTimeCard';

interface Variable {
  id: string;
  name: string;
  unit: string;
}

export function DashboardScreen(): React.ReactElement {
  // Usar el store de dashboard
  const {
    fetchTimeSeriesData,
    fetchScatterData,
    getTimeSeriesData,
    getScatterData,
    loading,
    error,
  } = useDashboardStore();

  // Variables disponibles (obtenidas desde CHART_VARIABLES del shared package)
  const [variables] = useState<Variable[]>(
    Object.values(CHART_VARIABLES).map(v => ({
      id: v.id,
      name: v.name,
      unit: v.unit,
    }))
  );

  // Estados independientes para cada componente
  const [timeSeriesVariable, setTimeSeriesVariable] = useState<string>('1');
  const [scatterVariableX, setScatterVariableX] = useState<string>('1');
  const [scatterVariableY, setScatterVariableY] = useState<string>('2');
  const [customVariable, setCustomVariable] = useState<string>('1');
  const [customChartType, setCustomChartType] = useState<ChartType>('line');

  // Cargar datos iniciales
  useEffect(() => {
    // Cargar datos para la variable inicial de series temporales
    fetchTimeSeriesData(timeSeriesVariable);
  }, []);

  // Cargar datos cuando cambie la variable de series temporales
  useEffect(() => {
    if (timeSeriesVariable) {
      fetchTimeSeriesData(timeSeriesVariable);
    }
  }, [timeSeriesVariable]);

  // Cargar datos cuando cambien las variables de scatter plot
  useEffect(() => {
    if (scatterVariableX && scatterVariableY) {
      fetchScatterData(scatterVariableX, scatterVariableY);
    }
  }, [scatterVariableX, scatterVariableY]);

  // Cargar datos cuando cambie la variable del gráfico personalizado
  useEffect(() => {
    if (customVariable) {
      fetchTimeSeriesData(customVariable);
    }
  }, [customVariable]);

  // Obtener datos desde el store
  const timeSeriesData = getTimeSeriesData(timeSeriesVariable);
  const scatterData = getScatterData(scatterVariableX, scatterVariableY);
  const customData = getTimeSeriesData(customVariable);

  const handleFilterChange = (filters: TimeFilters) => {
    console.log('Filtros aplicados:', filters);
    // Aquí podrías recargar los datos con los filtros aplicados
    // Por ejemplo: fetchTimeSeriesData(timeSeriesVariable, filters.days);
  };

  const handleExport = (filters: TimeFilters) => {
    console.log('Exportando con filtros:', filters);
    // Aquí implementarías la lógica de exportación
  };

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView style={styles.scrollView} showsVerticalScrollIndicator={false}>
        <View style={styles.header}>
          <Text variant="h1" color={semanticColors.primary} style={styles.title}>
            Dashboard de Monitoreo
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
            Análisis y visualización de datos en tiempo real
          </Text>
        </View>

        <View style={styles.content}>
          {/* Filtros de tiempo */}
          <FilterTimeCard
            onFilterChange={handleFilterChange}
            onExport={handleExport}
            style={styles.card}
          />

          {/* Gráfico de series temporales */}
          <TimeSeriesCard
            title="Series Temporales"
            variables={variables.map(v => ({ id: v.id, label: v.name, unit: v.unit }))}
            selectedVariable={timeSeriesVariable}
            onVariableChange={setTimeSeriesVariable}
            data={timeSeriesData?.map(d => ({ value: d.value, label: d.label || '' })) || []}
            isLoading={loading}
            error={error || undefined}
            style={styles.card}
          />

          {/* Gráfico de dispersión */}
          <ScatterPlotCard
            title="Gráfico de Dispersión"
            variables={variables.map(v => ({ id: v.id, label: v.name, unit: v.unit }))}
            selectedVariableX={scatterVariableX}
            selectedVariableY={scatterVariableY}
            onVariableXChange={setScatterVariableX}
            onVariableYChange={setScatterVariableY}
            data={scatterData || []}
            isLoading={loading}
            error={error || undefined}
            style={styles.card}
          />

          {/* Gráfico personalizable */}
          <CustomChartCard
            title="Gráfico Personalizable"
            variables={variables.map(v => ({ id: v.id, label: v.name, unit: v.unit }))}
            selectedVariable={customVariable}
            selectedChartType={customChartType}
            onVariableChange={setCustomVariable}
            onChartTypeChange={setCustomChartType}
            data={customData?.map(d => ({ value: d.value, label: d.label || '' })) || []}
            isLoading={loading}
            error={error || undefined}
            style={styles.card}
          />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  scrollView: {
    flex: 1,
  },
  header: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.md,
    backgroundColor: colors.hidro.bgLight,
  },
  content: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl,
  },
  title: {
    marginBottom: spacing.sm,
    textAlign: 'center',
  },
  description: {
    textAlign: 'center',
    lineHeight: 24,
  },
  card: {
    marginBottom: spacing.lg,
  },
});