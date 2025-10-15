import React, { useState, useEffect } from 'react';
import { View, StyleSheet, Dimensions, ViewStyle } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Card } from '../molecules/Card';
import { Text } from '../atoms/Text';
import { Select } from '../atoms/Select';
import { Icon } from '../atoms/Icon';
import { ScatterChart, ScatterDataPoint } from '../molecules/ScatterChart';

export interface ScatterPlotCardProps {
  variables?: VariableOption[];
  selectedVariableX?: string;
  selectedVariableY?: string;
  onVariableXChange?: (variableId: string) => void;
  onVariableYChange?: (variableId: string) => void;
  data?: ScatterDataPoint[];
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
  { id: 'turbidity', label: 'Turbidez', unit: 'NTU', color: '#DDA0DD' },
  { id: 'conductivity', label: 'Conductividad', unit: 'µS/cm', color: '#98D8C8' },
];

// Generar datos de dispersión de ejemplo con correlación realista
const generateScatterData = (varX: string, varY: string): ScatterDataPoint[] => {
  const baseValues: Record<string, number> = {
    temperature: 25,
    humidity: 60,
    pressure: 1013,
    ph: 7.2,
    dissolved_oxygen: 8.5,
    turbidity: 5,
    conductivity: 500,
  };

  const ranges: Record<string, number> = {
    temperature: 10,
    humidity: 30,
    pressure: 100,
    ph: 2,
    dissolved_oxygen: 4,
    turbidity: 8,
    conductivity: 200,
  };

  // Definir correlaciones realistas entre variables
  const correlations: Record<string, Record<string, number>> = {
    temperature: { humidity: -0.6, dissolved_oxygen: -0.7, ph: 0.3, pressure: -0.2, turbidity: 0.4, conductivity: 0.5 },
    humidity: { temperature: -0.6, dissolved_oxygen: -0.4, ph: -0.2, pressure: 0.1, turbidity: -0.3, conductivity: -0.2 },
    pressure: { temperature: -0.2, humidity: 0.1, dissolved_oxygen: 0.3, ph: 0.1, turbidity: -0.1, conductivity: 0.2 },
    ph: { temperature: 0.3, humidity: -0.2, pressure: 0.1, dissolved_oxygen: 0.5, turbidity: -0.6, conductivity: 0.4 },
    dissolved_oxygen: { temperature: -0.7, humidity: -0.4, pressure: 0.3, ph: 0.5, turbidity: -0.5, conductivity: -0.3 },
    turbidity: { temperature: 0.4, humidity: -0.3, pressure: -0.1, ph: -0.6, dissolved_oxygen: -0.5, conductivity: 0.2 },
    conductivity: { temperature: 0.5, humidity: -0.2, pressure: 0.2, ph: 0.4, dissolved_oxygen: -0.3, turbidity: 0.2 },
  };

  const baseX = baseValues[varX] || 50;
  const baseY = baseValues[varY] || 50;
  const rangeX = ranges[varX] || 20;
  const rangeY = ranges[varY] || 20;
  
  // Obtener la correlación esperada entre las variables
  const expectedCorrelation = correlations[varX]?.[varY] || correlations[varY]?.[varX] || 0;

  return Array.from({ length: 30 }, (_, i) => {
    // Generar valor X con distribución normal
    const randomX = (Math.random() + Math.random() + Math.random() + Math.random() - 2) / 2; // Aproximación a distribución normal
    const x = baseX + randomX * rangeX;
    
    // Generar valor Y correlacionado con X
    const randomY = (Math.random() + Math.random() + Math.random() + Math.random() - 2) / 2;
    const correlatedComponent = expectedCorrelation * (x - baseX) / rangeX;
    const noiseComponent = Math.sqrt(1 - expectedCorrelation * expectedCorrelation) * randomY;
    const y = baseY + (correlatedComponent + noiseComponent) * rangeY;
    
    return {
      value: x,
      value1: y,
      label: x.toFixed(1), // Usar el valor X como etiqueta
      // dataPointText: `(${x.toFixed(1)}, ${y.toFixed(1)})`, // Removed since labels are no longer displayed
    };
  });
};

export function ScatterPlotCard({
  variables = defaultVariables,
  selectedVariableX,
  selectedVariableY,
  onVariableXChange,
  onVariableYChange,
  data,
  isLoading = false,
  error,
  title = 'Gráfico de Dispersión',
  style,
}: ScatterPlotCardProps): React.ReactElement {
  const [variableX, setVariableX] = useState(
    selectedVariableX || variables[0]?.id || ''
  );
  const [variableY, setVariableY] = useState(
    selectedVariableY || variables[1]?.id || ''
  );
  const [scatterData, setScatterData] = useState<ScatterDataPoint[]>([]);

  // Sincronizar variableX cuando selectedVariableX cambia desde el padre
  useEffect(() => {
    if (selectedVariableX && selectedVariableX !== variableX) {
      setVariableX(selectedVariableX);
    }
  }, [selectedVariableX]);

  // Sincronizar variableY cuando selectedVariableY cambia desde el padre
  useEffect(() => {
    if (selectedVariableY && selectedVariableY !== variableY) {
      setVariableY(selectedVariableY);
    }
  }, [selectedVariableY]);

  useEffect(() => {
    // Validar datos externos: deben tener al menos un punto y valores numéricos
    const hasValidExternalData = Array.isArray(data) && data.length > 0 && data.every(d => typeof d.value === 'number' && (typeof d.value1 === 'number' || typeof d.label === 'string'));

    if (hasValidExternalData) {
      setScatterData(data as ScatterDataPoint[]);
    } else if (variableX && variableY && variableX !== variableY) {
      setScatterData(generateScatterData(variableX, variableY));
    } else {
      setScatterData([]);
    }
  }, [data, variableX, variableY]);

  const handleVariableXChange = (option: { label: string; value: string | number }) => {
    const variableId = String(option.value);
    setVariableX(variableId);
    onVariableXChange?.(variableId);
  };

  const handleVariableYChange = (option: { label: string; value: string | number }) => {
    const variableId = String(option.value);
    setVariableY(variableId);
    onVariableYChange?.(variableId);
  };

  const variableOptions = variables.map(variable => ({
    label: `${variable.label}${variable.unit ? ` (${variable.unit})` : ''}`,
    value: variable.id,
  }));

  const selectedVarXInfo = variables.find(v => v.id === variableX);
  const selectedVarYInfo = variables.find(v => v.id === variableY);

  const getStatistics = () => {
    if (scatterData.length === 0) return null;

    const xValues = scatterData.map(d => d.value);
    // Usar value1 si existe, de lo contrario usar value como fallback
    const yValues = scatterData.map(d => (d.value1 !== undefined ? d.value1 : d.value));

    const xMin = Math.min(...xValues);
    const xMax = Math.max(...xValues);
    const xAvg = xValues.reduce((sum, val) => sum + val, 0) / xValues.length;

    const yMin = Math.min(...yValues);
    const yMax = Math.max(...yValues);
    const yAvg = yValues.reduce((sum, val) => sum + val, 0) / yValues.length;

    // Calcular correlación simple
    const correlation = calculateCorrelation(xValues, yValues);

    return { 
      x: { min: xMin, max: xMax, avg: xAvg },
      y: { min: yMin, max: yMax, avg: yAvg },
      correlation 
    };
  };

  const calculateCorrelation = (x: number[], y: number[]): number => {
    const n = x.length;
    const sumX = x.reduce((a, b) => a + b, 0);
    const sumY = y.reduce((a, b) => a + b, 0);
    const sumXY = x.reduce((sum, xi, i) => sum + xi * (y[i] || 0), 0);
    const sumX2 = x.reduce((sum, xi) => sum + xi * xi, 0);
    const sumY2 = y.reduce((sum, yi) => sum + yi * yi, 0);

    const numerator = n * sumXY - sumX * sumY;
    const denominator = Math.sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

    return denominator === 0 ? 0 : numerator / denominator;
  };

  const stats = getStatistics();

  const getCorrelationDescription = (correlation: number): string => {
    const abs = Math.abs(correlation);
    if (abs > 0.7) return 'Fuerte';
    if (abs > 0.4) return 'Moderada';
    if (abs > 0.1) return 'Débil';
    return 'Muy débil';
  };

  return (
    <Card style={StyleSheet.flatten([styles.card, style])}>
      <View style={styles.header}>
        <Icon name="info" size={20} color={semanticColors.primary} />
        <Text variant="h3" style={styles.title}>
          {title}
        </Text>
      </View>

      <View style={styles.content}>
        {/* Selectores de variables */}
        <View style={styles.variableSelectors}>
          <View style={styles.selectorContainer}>
            <Text variant="label" style={styles.sectionTitle}>
              Eje X
            </Text>
            <Select
              options={variableOptions}
              value={variableX}
              onSelect={handleVariableXChange}
              placeholder="Seleccionar variable X"
            />
          </View>

          <View style={styles.selectorContainer}>
            <Text variant="label" style={styles.sectionTitle}>
              Eje Y
            </Text>
            <Select
              options={variableOptions}
              value={variableY}
              onSelect={handleVariableYChange}
              placeholder="Seleccionar variable Y"
            />
          </View>
        </View>

        {/* Validación de variables */}
        {variableX === variableY && variableX && (
          <View style={styles.warningContainer}>
            <Icon name="warning" size={18} color={semanticColors.warningText} />
            <View style={styles.warningTextContainer}>
              <Text variant="body" color={semanticColors.warningText} style={styles.warningTitle}>
                Variables idénticas seleccionadas
              </Text>
              <Text variant="caption" color={semanticColors.warningText}>
                Para crear un gráfico de dispersión significativo, selecciona variables diferentes para los ejes X e Y. 
                Esto te permitirá analizar la correlación entre dos variables distintas.
              </Text>
            </View>
          </View>
        )}

        {/* Estadísticas */}
        {stats && (
          <View style={styles.statsContainer}>
            <View style={styles.statsRow}>
              <View style={styles.statGroup}>
                <Text variant="caption" color={semanticColors.textSecondary} style={styles.statGroupTitle}>
                  {selectedVarXInfo?.label} (X)
                </Text>
                <View style={styles.statItems}>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Min</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.x.min.toFixed(1)}
                    </Text>
                  </View>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Prom</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.x.avg.toFixed(1)}
                    </Text>
                  </View>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Max</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.x.max.toFixed(1)}
                    </Text>
                  </View>
                </View>
              </View>

              <View style={styles.statGroup}>
                <Text variant="caption" color={semanticColors.textSecondary} style={styles.statGroupTitle}>
                  {selectedVarYInfo?.label} (Y)
                </Text>
                <View style={styles.statItems}>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Min</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.y.min.toFixed(1)}
                    </Text>
                  </View>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Prom</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.y.avg.toFixed(1)}
                    </Text>
                  </View>
                  <View style={styles.statItem}>
                    <Text variant="caption" color={semanticColors.textSecondary}>Max</Text>
                    <Text variant="caption" style={styles.statValue}>
                      {stats.y.max.toFixed(1)}
                    </Text>
                  </View>
                </View>
              </View>
            </View>

            <View style={styles.correlationContainer}>
              <Text variant="caption" color={semanticColors.textSecondary}>
                Correlación: 
              </Text>
              <Text variant="body" style={styles.correlationValue}>
                {stats.correlation.toFixed(3)} ({getCorrelationDescription(stats.correlation)})
              </Text>
            </View>
          </View>
        )}

        {/* Gráfico de dispersión */}
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
          ) : scatterData.length > 0 ? (
            <ScatterChart
              key={`${variableX}-${variableY}-${scatterData.length}`}
              data={scatterData}
              height={220}
              color={selectedVarXInfo?.color || selectedVarYInfo?.color || semanticColors.primary}
              dataPointsRadius={7}
              showLine={false}
              showXAxisLabels={true}
              showYAxisLabels={true}
              showVerticalLines={true}
              isAnimated={true}
              animationDuration={800}
            />
          ) : (
            <View style={styles.placeholderContainer}>
              <Icon name="info" size={48} color={semanticColors.textSecondary} />
              <Text variant="body" color={semanticColors.textSecondary} style={styles.placeholderText}>
                Selecciona dos variables diferentes para ver el gráfico de dispersión
              </Text>
            </View>
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
  variableSelectors: {
    gap: spacing.md,
  },
  selectorContainer: {
    gap: spacing.sm,
  },
  sectionTitle: {
    color: semanticColors.textPrimary,
    fontWeight: '600',
  },
  warningContainer: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
    backgroundColor: semanticColors.warningBg,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: semanticColors.warningText,
  },
  warningTextContainer: {
    flex: 1,
    gap: spacing.xs,
  },
  warningTitle: {
    fontWeight: '600',
  },
  statsContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
    gap: spacing.md,
  },
  statsRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
  },
  statGroup: {
    alignItems: 'center',
    gap: spacing.sm,
  },
  statGroupTitle: {
    fontWeight: '600',
  },
  statItems: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  statItem: {
    alignItems: 'center',
    gap: spacing.xs,
  },
  statValue: {
    fontWeight: '600',
    color: semanticColors.textPrimary,
  },
  correlationContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.sm,
  },
  correlationValue: {
    fontWeight: '600',
    color: semanticColors.primary,
  },
  chartContainer: {
    alignItems: 'center',
    minHeight: 260, // Aumentado para dar espacio a las etiquetas del eje X
    justifyContent: 'center',
    width: '100%',
    paddingBottom: 20, // Espacio adicional para las etiquetas
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