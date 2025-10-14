import React, { useState } from 'react';
import { View, StyleSheet, LayoutChangeEvent } from 'react-native';
import { VictoryChart, VictoryLine, VictoryAxis, VictoryTheme, VictoryContainer } from 'victory-native';
import { semanticColors, spacing, useFuzzyStore } from '@hidroespinaca/shared';
import type { SimpleFuzzyVariable, SimpleFuzzyTerm } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';

interface MembershipChartProps {
  variable: SimpleFuzzyVariable;
}

export const MembershipChart: React.FC<MembershipChartProps> = ({ variable }) => {
  const [containerWidth, setContainerWidth] = useState(300);
  const { getTermsByVariableId } = useFuzzyStore();
  
  const handleLayout = (event: LayoutChangeEvent) => {
    const { width } = event.nativeEvent.layout;
    setContainerWidth(width);
  };
  
  // Calcular el ancho del gráfico basado en el contenedor
  const chartWidth = Math.max(200, containerWidth - (spacing.md * 2));

  // Obtener los términos de la variable
  const terms = getTermsByVariableId(variable.id);
  
  // Obtener el rango de valores de los términos
  const minValue = terms.length > 0 ? (terms[0]?.membership_function?.universe_min ?? 0) : 0;
  const maxValue = terms.length > 0 ? (terms[0]?.membership_function?.universe_max ?? 100) : 100;

  const generateTermData = (term: SimpleFuzzyTerm, min: number, max: number) => {
    const steps = 40;
    const stepSize = (max - min) / steps;
    const data: { x: number; y: number }[] = [];
    const params = term.membership_function.parameters;

    // Validate parameters exist
    if (!params || params.length === 0) {
      return data;
    }

    for (let i = 0; i <= steps; i++) {
      const x = min + i * stepSize;
      let y = 0;

      switch (term.membership_function.function_type) {
        case 'triangular':
          if (params.length >= 3 && params[0] !== undefined && params[1] !== undefined && params[2] !== undefined) {
            if (x <= params[0] || x >= params[2]) {
              y = 0;
            } else if (x <= params[1]) {
              y = (x - params[0]) / (params[1] - params[0]);
            } else {
              y = (params[2] - x) / (params[2] - params[1]);
            }
          }
          break;
        case 'trapezoidal':
          if (params.length >= 4 && params[0] !== undefined && params[1] !== undefined && params[2] !== undefined && params[3] !== undefined) {
            if (x <= params[0] || x >= params[3]) {
              y = 0;
            } else if (x <= params[1]) {
              y = (x - params[0]) / (params[1] - params[0]);
            } else if (x <= params[2]) {
              y = 1;
            } else {
              y = (params[3] - x) / (params[3] - params[2]);
            }
          }
          break;
        case 'gaussian':
          if (params.length >= 2 && params[0] !== undefined && params[1] !== undefined) {
            y = Math.exp(-0.5 * Math.pow((x - params[0]) / params[1], 2));
          }
          break;
        default:
          y = 0;
      }

      data.push({ x, y });
    }

    return data;
  };

  // Paleta de colores vibrantes para asignación dinámica
  const colorPalette = [
    '#2563eb', // Azul vibrante
    '#dc2626', // Rojo vibrante
    '#16a34a', // Verde vibrante
    '#7c3aed', // Púrpura vibrante
    '#ea580c', // Naranja vibrante
    '#0891b2', // Cian vibrante
    '#be185d', // Rosa vibrante
    '#65a30d', // Lima vibrante
    '#4338ca', // Índigo vibrante
    '#c2410c', // Rojo-naranja vibrante
  ];

  // Colores para los términos
  function getTermColor(termName: string, index: number): string {
    // Mapeo específico para términos conocidos
    const specificColorMap: { [key: string]: string } = {
      'baja': '#2563eb',     // Azul vibrante
      'media': '#dc2626',    // Rojo vibrante
      'alta': '#16a34a',     // Verde vibrante
      'bajo': '#2563eb',     // Azul vibrante
      'medio': '#dc2626',    // Rojo vibrante
      'alto': '#16a34a',     // Verde vibrante
    };

    const lowerTermName = termName.toLowerCase();
    
    // Si el término tiene un color específico, usarlo
    if (specificColorMap[lowerTermName]) {
      return specificColorMap[lowerTermName];
    }
    
    // Para términos no conocidos, asignar color dinámicamente basado en el índice
    return colorPalette[index % colorPalette.length] || '#2563eb';
  }

  // Generar datos para cada término
  const termData = terms.map((term: SimpleFuzzyTerm, index: number) => ({
    data: generateTermData(term, minValue, maxValue),
    name: term.label,
    color: getTermColor(term.label, index)
  }));

  return (
    <View style={styles.container} onLayout={handleLayout}>
      <Text variant="h3" style={styles.title}>
        {variable.name}
      </Text>
      
      {containerWidth > 0 && (
        <VictoryChart
          theme={VictoryTheme.material}
          width={chartWidth}
          height={200}
          padding={{ left: 50, top: 20, right: 20, bottom: 50 }}
          containerComponent={<VictoryContainer responsive={false} />}
        >
          <VictoryAxis
            dependentAxis
            tickFormat={(t) => `${t}`}
            style={{
              axis: { stroke: semanticColors.border },
              tickLabels: { fontSize: 12, fill: semanticColors.textPrimary },
              grid: { stroke: semanticColors.border, strokeOpacity: 0.3 }
            }}
          />
          <VictoryAxis
            tickFormat={(t) => `${t}`}
            style={{
              axis: { stroke: semanticColors.border },
              tickLabels: { fontSize: 12, fill: semanticColors.textPrimary },
              grid: { stroke: semanticColors.border, strokeOpacity: 0.3 }
            }}
          />
          
          {termData.map((term, index) => (
            <VictoryLine
              key={index}
              data={term.data}
              style={{
                data: { stroke: term.color, strokeWidth: 2 }
              }}
              animate={{
                duration: 1000,
                onLoad: { duration: 500 }
              }}
            />
          ))}
        </VictoryChart>
      )}
      
      <View style={styles.legend}>
        {termData.map((term, index) => (
          <View key={index} style={styles.legendItem}>
            <View style={[styles.legendColor, { backgroundColor: term.color }]} />
            <Text variant="caption" style={styles.legendText}>
              {term.name}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.backgroundPrimary,
    borderRadius: 8,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: semanticColors.border,
  },
  title: {
    fontWeight: '600',
    marginBottom: spacing.md,
    textAlign: 'center',
    fontSize: 16,
  },
  legend: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: spacing.sm,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  legendColor: {
    width: 12,
    height: 12,
    borderRadius: 2,
  },
  legendText: {
    fontSize: 12,
  },
});