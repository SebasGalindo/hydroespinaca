import React, { useState } from 'react';
import { View, StyleSheet, LayoutChangeEvent } from 'react-native';
import { VictoryChart, VictoryBar, VictoryAxis, VictoryTheme, VictoryContainer } from 'victory-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';

export interface BarChartDataPoint {
  value: number;
  label?: string;
  dataPointText?: string;
  labelTextStyle?: object;
  dataPointColor?: string;
}

export interface BarChartProps {
  data: BarChartDataPoint[];
  height?: number;
  color?: string;
  showXAxisLabels?: boolean;
  showYAxisLabels?: boolean;
  yAxisLabelWidth?: number;
  xAxisLabelTextStyle?: object;
  yAxisLabelTextStyle?: object;
  animationDuration?: number;
  maxValue?: number;
  backgroundColor?: string;
  isAnimated?: boolean;
  barWidth?: number;
  cornerRadius?: number;
}

export function BarChart({
  data,
  height = 200,
  color = semanticColors.primary,
  showXAxisLabels = true,
  showYAxisLabels = true,
  yAxisLabelWidth = 40,
  xAxisLabelTextStyle,
  yAxisLabelTextStyle,
  animationDuration = 1000,
  maxValue,
  backgroundColor = 'transparent',
  isAnimated = true,
  barWidth = 20,
  cornerRadius = 4,
}: BarChartProps): React.ReactElement {
  const [containerWidth, setContainerWidth] = useState(300);

  const handleLayout = (event: LayoutChangeEvent) => {
    const { width } = event.nativeEvent.layout;
    setContainerWidth(width);
  };

  // Calcular el ancho del gráfico basado en el contenedor
  const chartWidth = Math.max(200, containerWidth - (spacing.md * 2));

  // Limitar la cantidad de datos mostrados para evitar saturación
  const maxDataPoints = 12;
  const displayData = data.length > maxDataPoints ? data.slice(0, maxDataPoints) : data;

  // Convertir datos al formato de Victory
  const victoryData = displayData.map((point, index) => ({
    x: index + 1,
    y: point.value,
    label: point.label,
  }));

  // Mostrar solo algunas etiquetas del eje X para evitar sobresaturación
  const showEveryNthLabel = Math.ceil(displayData.length / 6);

  return (
    <View style={[styles.container, { backgroundColor }]} onLayout={handleLayout}>
      {containerWidth > 0 && (
        <VictoryChart
          theme={VictoryTheme.material}
          width={chartWidth}
          height={height}
          padding={{ left: yAxisLabelWidth + 10, top: 20, right: 20, bottom: showXAxisLabels ? 40 : 20 }}
          containerComponent={<VictoryContainer responsive={false} />}
          domainPadding={{ x: 15 }}
        >
          {showYAxisLabels && (
            <VictoryAxis
              dependentAxis
              tickFormat={(t) => Math.round(t).toString()}
              style={{
                axis: { stroke: semanticColors.border },
                tickLabels: { 
                  fontSize: 10, 
                  fill: semanticColors.textSecondary,
                  ...yAxisLabelTextStyle 
                },
                grid: { 
                  stroke: semanticColors.border, 
                  strokeOpacity: 0.2,
                  strokeDasharray: '2,2'
                }
              }}
              domain={maxValue ? [0, maxValue] : undefined}
            />
          )}
          
          {showXAxisLabels && (
            <VictoryAxis
              tickFormat={(t, i) => {
                // Mostrar solo algunas etiquetas para evitar aglomeración
                return i % showEveryNthLabel === 0 && displayData[i]?.label 
                  ? displayData[i].label 
                  : '';
              }}
              style={{
                axis: { stroke: semanticColors.border },
                tickLabels: { 
                  fontSize: 9, 
                  fill: semanticColors.textSecondary,
                  ...xAxisLabelTextStyle 
                },
                grid: { stroke: 'transparent' }
              }}
            />
          )}
          
          <VictoryBar
            data={victoryData}
            style={{
              data: { 
                fill: color,
              }
            }}
            labels={() => null}
            cornerRadius={{ top: cornerRadius }}
            animate={isAnimated ? {
              duration: animationDuration,
              onLoad: { duration: 500 }
            } : false}
          />
        </VictoryChart>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    flex: 1,
  },
});
