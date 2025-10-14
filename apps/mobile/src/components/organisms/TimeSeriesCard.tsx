import React, { useState, useEffect } from 'react';
import { View, StyleSheet, Dimensions, ViewStyle } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Card } from '../molecules/Card';
import { Text } from '../atoms/Text';
import { Select } from '../atoms/Select';
import { Icon } from '../atoms/Icon';
import { LineChart, LineChartDataPoint } from '../molecules/LineChart';

export interface TimeSeriesCardProps {
  variables?: VariableOption[];
  selectedVariable?: string;
  onVariableChange?: (variableId: string) => void;
  data?: LineChartDataPoint[];
  isLoading?: boolean;
  error?: string;
  title?: string;
  style?: ViewStyle;
}

export interface VariableOption {
  id: string;
  label: string;
  unit?: string;
  color?: string;
}

const defaultVariables: VariableOption[] = [
  { id: 'temperature', label: 'Temperatura', unit: '°C', color: '#FF6B6B' },
  { id: 'humidity', label: 'Humedad', unit: '%', color: '#4ECDC4' },
  { id: 'pressure', label: 'Presión', unit: 'hPa', color: '#45B7D1' },
  { id: 'ph', label: 'pH', unit: 'pH', color: '#96CEB4' },
  { id: 'dissolved_oxygen', label: 'Oxígeno Disuelto', unit: 'mg/L', color: '#FFEAA7' },
];

// Datos de ejemplo para demostración
const generateSampleData = (variableId: string): LineChartDataPoint[] => {
  const baseValues: Record<string, number> = {
    temperature: 25,
    humidity: 60,
    pressure: 1013,
    ph: 7.2,
    dissolved_oxygen: 8.5,
  };

  const variations: Record<string, number> = {
    temperature: 5,
    humidity: 20,
    pressure: 50,
    ph: 1,
    dissolved_oxygen: 2,
  };

  const base = baseValues[variableId] || 50;
  const variation = variations[variableId] || 10;

  return Array.from({ length: 24 }, (_, i) => ({
    value: base + (Math.sin(i / 4) * variation) + (Math.random() - 0.5) * variation * 0.5,
    label: `${i}:00`,
  }));
};

export function TimeSeriesCard({
  variables = defaultVariables,
  selectedVariable,
  onVariableChange,
  data,
  isLoading = false,
  error,
  title = 'Series de Tiempo',
  style,
}: TimeSeriesCardProps): React.ReactElement {
  const [currentVariable, setCurrentVariable] = useState(
    selectedVariable || variables[0]?.id || ''
  );
  const [chartData, setChartData] = useState<LineChartDataPoint[]>([]);

  // Sincronizar currentVariable cuando selectedVariable cambia desde el padre
  useEffect(() => {
    if (selectedVariable && selectedVariable !== currentVariable) {
      setCurrentVariable(selectedVariable);
    }
  }, [selectedVariable]);

  useEffect(() => {
    if (data) {
      setChartData(data);
    } else if (currentVariable) {
      // Generar datos de ejemplo si no se proporcionan datos reales
      setChartData(generateSampleData(currentVariable));
    }
  }, [data, currentVariable]);

  const handleVariableChange = (option: { label: string; value: string | number }) => {
    const variableId = String(option.value);
    setCurrentVariable(variableId);
    onVariableChange?.(variableId);
  };

  const selectedVariableInfo = variables.find(v => v.id === currentVariable);
  const chartColor = selectedVariableInfo?.color || semanticColors.primary;

  const variableOptions = variables.map(variable => ({
    label: `${variable.label}${variable.unit ? ` (${variable.unit})` : ''}`,
    value: variable.id,
  }));

  const getStatistics = () => {
    if (chartData.length === 0) return null;

    const values = chartData.map(d => d.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const avg = values.reduce((sum, val) => sum + val, 0) / values.length;

    return { min, max, avg };
  };

  const stats = getStatistics();

  return (
    <Card style={StyleSheet.flatten([styles.card, style])}>
      <View style={styles.header}>
        <Icon name="trending-up" size={20} color={semanticColors.primary} />
        <Text variant="h3" style={styles.title}>
          {title}
        </Text>
      </View>

      <View style={styles.content}>
        {/* Selector de variable */}
        <View style={styles.section}>
          <Text variant="label" style={styles.sectionTitle}>
            Variable a Monitorear
          </Text>
          <Select
            options={variableOptions}
            value={currentVariable}
            onSelect={handleVariableChange}
            placeholder="Seleccionar variable"
          />
        </View>

        {/* Estadísticas rápidas */}
        {stats && (
          <View style={styles.statsContainer}>
            <View style={styles.statItem}>
              <Text variant="caption" color={semanticColors.textSecondary}>
                Mínimo
              </Text>
              <Text variant="body" style={styles.statValue}>
                {stats.min.toFixed(1)}
                {selectedVariableInfo?.unit && ` ${selectedVariableInfo.unit}`}
              </Text>
            </View>
            <View style={styles.statItem}>
              <Text variant="caption" color={semanticColors.textSecondary}>
                Promedio
              </Text>
              <Text variant="body" style={styles.statValue}>
                {stats.avg.toFixed(1)}
                {selectedVariableInfo?.unit && ` ${selectedVariableInfo.unit}`}
              </Text>
            </View>
            <View style={styles.statItem}>
              <Text variant="caption" color={semanticColors.textSecondary}>
                Máximo
              </Text>
              <Text variant="body" style={styles.statValue}>
                {stats.max.toFixed(1)}
                {selectedVariableInfo?.unit && ` ${selectedVariableInfo.unit}`}
              </Text>
            </View>
          </View>
        )}

        {/* Gráfico */}
        <View style={styles.chartContainer}>
          {error ? (
            <View style={styles.errorContainer}>
              <Icon name="warning" size={24} color={semanticColors.errorBg} />
              <Text variant="body" color={semanticColors.errorBg} style={styles.errorText}>
                {error}
              </Text>
            </View>
          ) : isLoading ? (
            <View style={styles.loadingContainer}>
              <Text variant="body" color={semanticColors.textSecondary}>
                Cargando datos...
              </Text>
            </View>
          ) : (
            <LineChart
              key={`${currentVariable}-${chartData.length}`}
              data={chartData}
              height={200}
              color={chartColor}
              curved={true}
              showDataPoints={true}
              isAnimated={true}
              animationDuration={1000}
            />
          )}
        </View>


      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    marginBottom: spacing.lg,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  title: {
    marginLeft: spacing.sm,
    color: semanticColors.textPrimary,
  },
  content: {
    gap: spacing.md,
  },
  section: {
    gap: spacing.sm,
  },
  sectionTitle: {
    color: semanticColors.textPrimary,
    fontWeight: '600',
  },
  statsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
  },
  statItem: {
    alignItems: 'center',
    gap: spacing.xs,
  },
  statValue: {
    fontWeight: '600',
    color: semanticColors.textPrimary,
  },
  chartContainer: {
    alignItems: 'center',
    minHeight: 220,
    justifyContent: 'center',
    width: '100%',
    overflow: 'hidden',
  },
  errorContainer: {
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.lg,
  },
  errorText: {
    textAlign: 'center',
  },
  loadingContainer: {
    alignItems: 'center',
    paddingVertical: spacing.lg,
  },
  infoContainer: {
    alignItems: 'center',
  },
});