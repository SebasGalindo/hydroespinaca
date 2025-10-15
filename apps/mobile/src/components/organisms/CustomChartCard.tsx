import React, { useState, useEffect } from 'react';
import { View, StyleSheet, Dimensions, ViewStyle } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Card } from '../molecules/Card';
import { Text } from '../atoms/Text';
import { Select } from '../atoms/Select';
import { Icon } from '../atoms/Icon';
import { IconName } from '@hydroespinaca/shared';
import { LineChart, LineChartDataPoint } from '../molecules/LineChart';
import { ScatterChart, ScatterDataPoint } from '../molecules/ScatterChart';
import { BarChart, BarChartDataPoint } from '../molecules/BarChart';
import { AreaChart, AreaChartDataPoint } from '../molecules/AreaChart';


export interface CustomChartCardProps {
  variables?: VariableOption[];
  selectedVariable?: string;
  onVariableChange?: (variableId: string) => void;
  selectedChartType?: ChartType;
  onChartTypeChange?: (chartType: ChartType) => void;
  data?: any[];
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

export type ChartType = 'line' | 'scatter' | 'bar' | 'area';

const defaultVariables: VariableOption[] = [
  { id: 'temperature', label: 'Temperatura', unit: '°C', color: '#FF6B6B' },
  { id: 'humidity', label: 'Humedad', unit: '%', color: '#4ECDC4' },
  { id: 'pressure', label: 'Presión', unit: 'hPa', color: '#45B7D1' },
  { id: 'ph', label: 'pH', unit: 'pH', color: '#96CEB4' },
  { id: 'dissolved_oxygen', label: 'Oxígeno Disuelto', unit: 'mg/L', color: '#FFEAA7' },
  { id: 'turbidity', label: 'Turbidez', unit: 'NTU', color: '#DDA0DD' },
  { id: 'conductivity', label: 'Conductividad', unit: 'µS/cm', color: '#98D8C8' },
];

const chartTypeOptions = [
  { label: 'Línea', value: 'line', icon: 'trending-up' },
  { label: 'Dispersión', value: 'scatter', icon: 'info' },
  { label: 'Barras', value: 'bar', icon: 'chart' },
  { label: 'Área', value: 'area', icon: 'chart' },
];

// Generar datos de ejemplo según el tipo de gráfico
const generateChartData = (variableId: string, chartType: ChartType): any[] => {
  const baseValues: Record<string, number> = {
    temperature: 25,
    humidity: 60,
    pressure: 1013,
    ph: 7.2,
    dissolved_oxygen: 8.5,
    turbidity: 5,
    conductivity: 500,
  };

  const variations: Record<string, number> = {
    temperature: 5,
    humidity: 20,
    pressure: 50,
    ph: 1,
    dissolved_oxygen: 2,
    turbidity: 3,
    conductivity: 100,
  };

  const base = baseValues[variableId] || 50;
  const variation = variations[variableId] || 10;

  switch (chartType) {
    case 'line':
    case 'area':
      return Array.from({ length: 24 }, (_, i) => ({
        value: base + (Math.sin(i / 4) * variation) + (Math.random() - 0.5) * variation * 0.5,
        label: `${i}:00`,
      }));

    case 'scatter':
      return Array.from({ length: 20 }, (_, i) => ({
        value: base + (Math.random() - 0.5) * variation * 2,
        value1: base + (Math.random() - 0.5) * variation * 2,
        label: `P${i + 1}`,
      }));

    case 'bar':
      return Array.from({ length: 10 }, (_, i) => ({
        value: base + (Math.random() - 0.5) * variation,
        label: `${i + 1}`,
      }));

    default:
      return [];
  }
};

export function CustomChartCard({
  variables = defaultVariables,
  selectedVariable,
  onVariableChange,
  selectedChartType = 'line',
  onChartTypeChange,
  data,
  isLoading = false,
  error,
  title = 'Gráfico Personalizado',
  style,
}: CustomChartCardProps): React.ReactElement {
  const [variable, setVariable] = useState(
    selectedVariable || variables[0]?.id || ''
  );
  const [chartType, setChartType] = useState<ChartType>(selectedChartType);
  const [chartData, setChartData] = useState<any[]>([]);

  // Sincronizar variable cuando selectedVariable cambia desde el padre
  useEffect(() => {
    if (selectedVariable && selectedVariable !== variable) {
      setVariable(selectedVariable);
    }
  }, [selectedVariable]);

  // Sincronizar chartType cuando selectedChartType cambia desde el padre
  useEffect(() => {
    if (selectedChartType && selectedChartType !== chartType) {
      setChartType(selectedChartType);
    }
  }, [selectedChartType]);

  useEffect(() => {
    if (data) {
      setChartData(data);
    } else if (variable) {
      setChartData(generateChartData(variable, chartType));
    }
  }, [data, variable, chartType]);

  const handleVariableChange = (option: { label: string; value: string | number }) => {
    const value = String(option.value);
    setVariable(value);
    onVariableChange?.(value);
  };

  const handleChartTypeChange = (option: { label: string; value: string | number }) => {
    const newChartType = String(option.value) as ChartType;
    setChartType(newChartType);
    onChartTypeChange?.(newChartType);
  };

  const variableOptions = variables.map(variable => ({
    label: `${variable.label}${variable.unit ? ` (${variable.unit})` : ''}`,
    value: variable.id,
  }));

  const selectedVariableInfo = variables.find(v => v.id === variable);
  const chartColor = selectedVariableInfo?.color || semanticColors.primary;

  const getStatistics = () => {
    if (chartData.length === 0) return null;

    const values = chartData.map(d => d.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const avg = values.reduce((sum, val) => sum + val, 0) / values.length;

    return { min, max, avg, count: values.length };
  };

  const stats = getStatistics();

  const renderChart = () => {
    if (error) {
      return (
        <View style={styles.errorContainer}>
          <Icon name="warning" size={24} color={semanticColors.errorBg} />
              <Text variant="body" color={semanticColors.errorBg} style={styles.errorText}>
            {error}
          </Text>
        </View>
      );
    }

    if (isLoading) {
      return (
        <View style={styles.loadingContainer}>
          <Text variant="body" color={semanticColors.textSecondary}>
            Cargando datos...
          </Text>
        </View>
      );
    }

    if (chartData.length === 0) {
      return (
        <View style={styles.placeholderContainer}>
          <Icon name="chart" size={48} color={semanticColors.textSecondary} />
          <Text variant="body" color={semanticColors.textSecondary} style={styles.placeholderText}>
            Selecciona una variable para ver el gráfico
          </Text>
        </View>
      );
    }

    const commonProps = {
      height: 200,
      color: chartColor,
      isAnimated: true,
      animationDuration: 1000,
    };

    switch (chartType) {
      case 'line':
        return (
          <LineChart
            key={`line-${variable}-${chartData.length}`}
            data={chartData as LineChartDataPoint[]}
            {...commonProps}
            curved={true}
            showDataPoints={true}
          />
        );

      case 'area':
        return (
          <AreaChart
            key={`area-${variable}-${chartData.length}`}
            data={chartData as AreaChartDataPoint[]}
            {...commonProps}
            fillOpacity={0.3}
            interpolation="cardinal"
          />
        );

      case 'bar':
        return (
          <BarChart
            key={`bar-${variable}-${chartData.length}`}
            data={chartData as BarChartDataPoint[]}
            {...commonProps}
            cornerRadius={4}
          />
        );

      case 'scatter':
        return (
          <ScatterChart
            key={`scatter-${variable}-${chartData.length}`}
            data={chartData as ScatterDataPoint[]}
            {...commonProps}
            dataPointsRadius={6}
            showLine={false}
          />
        );



      default:
        return null;
    }
  };

  const getChartTypeIcon = (type: ChartType): IconName => {
    const option = chartTypeOptions.find(opt => opt.value === type);
    return (option?.icon || 'chart') as IconName;
  };

  return (
    <Card style={StyleSheet.flatten([styles.card, style])}>
      <View style={styles.header}>
        <Icon name={getChartTypeIcon(chartType)} size={20} color={semanticColors.primary} />
        <Text variant="h3" style={styles.title}>
          {title}
        </Text>
      </View>

      <View style={styles.content}>
        {/* Selectores */}
        <View style={styles.selectors}>
          <View style={styles.selectorContainer}>
            <Text variant="label" style={styles.sectionTitle}>
              Variable
            </Text>
            <Select
              options={variableOptions}
              value={variable}
              onSelect={handleVariableChange}
              placeholder="Seleccionar variable"
            />
          </View>

          <View style={styles.selectorContainer}>
            <Text variant="label" style={styles.sectionTitle}>
              Tipo de Gráfico
            </Text>
            <Select
              options={chartTypeOptions.map(opt => ({ label: opt.label, value: opt.value }))}
              value={chartType}
              onSelect={handleChartTypeChange}
              placeholder="Seleccionar tipo"
            />
          </View>
        </View>

        {/* Estadísticas */}
        {stats && (
          <View style={styles.statsContainer}>
            <View style={styles.statsGrid}>
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
              <View style={styles.statItem}>
                <Text variant="caption" color={semanticColors.textSecondary}>
                  Puntos
                </Text>
                <Text variant="body" style={styles.statValue}>
                  {stats.count}
                </Text>
              </View>
            </View>
          </View>
        )}

        {/* Gráfico */}
        <View style={styles.chartContainer}>
          {renderChart()}
        </View>

        {/* Información adicional */}
        {chartData.length > 0 && (
          <View style={styles.infoContainer}>
            <Text variant="caption" color={semanticColors.textSecondary}>
              {selectedVariableInfo?.label} • Tipo: {chartTypeOptions.find(opt => opt.value === chartType)?.label}
            </Text>
          </View>
        )}
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
  selectors: {
    gap: spacing.md,
  },
  selectorContainer: {
    gap: spacing.sm,
  },
  sectionTitle: {
    color: semanticColors.textPrimary,
    fontWeight: '600',
  },
  statsContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
  },
  statsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    flexWrap: 'wrap',
    gap: spacing.sm,
  },
  statItem: {
    alignItems: 'center',
    gap: spacing.xs,
    minWidth: '20%',
  },
  statValue: {
    fontWeight: '600',
    color: semanticColors.textPrimary,
    textAlign: 'center',
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
  placeholderContainer: {
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.lg,
    paddingHorizontal: spacing.md,
  },
  placeholderText: {
    textAlign: 'center',
    lineHeight: 20,
  },
  infoContainer: {
    alignItems: 'center',
  },
});