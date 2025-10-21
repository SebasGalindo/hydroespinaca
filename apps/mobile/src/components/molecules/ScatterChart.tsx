import React, { useState } from 'react';
import { View, StyleSheet, LayoutChangeEvent } from 'react-native';
import { VictoryChart, VictoryScatter, VictoryAxis, VictoryTheme, VictoryContainer, VictoryLine } from 'victory-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';

export interface ScatterDataPoint {
  value: number;
  value1?: number;
  label?: string;
  dataPointText?: string;
  dataPointColor?: string;
  dataPointRadius?: number;
}

export interface ScatterChartProps {
  data: ScatterDataPoint[];
  height?: number;
  color?: string;
  dataPointsRadius?: number;
  showLine?: boolean;
  showVerticalLines?: boolean;
  showXAxisLabels?: boolean;
  showYAxisLabels?: boolean;
  yAxisLabelWidth?: number;
  xAxisLabelTextStyle?: object;
  maxValue?: number;
  stepValue?: number;
  noOfSections?: number;
  backgroundColor?: string;
  isAnimated?: boolean;
  animationDuration?: number;
}

export function ScatterChart({
  data,
  height = 200,
  color = semanticColors.primary,
  dataPointsRadius = 8,
  showLine = false,
  showVerticalLines = true,
  showXAxisLabels = true,
  showYAxisLabels = true,
  yAxisLabelWidth = 50,
  xAxisLabelTextStyle,
  maxValue,
  stepValue,
  noOfSections = 6,
  backgroundColor = 'transparent',
  isAnimated = true,
  animationDuration = 800,
}: ScatterChartProps): React.ReactElement {
  const [containerWidth, setContainerWidth] = useState(300);

  const handleLayout = (event: LayoutChangeEvent) => {
    const { width } = event.nativeEvent.layout;
    setContainerWidth(width);
  };

  // Calcular el ancho del gráfico basado en el contenedor
  const chartWidth = Math.max(200, containerWidth - (spacing.md * 2));

  // Convertir datos al formato de Victory
  const victoryData = data.map((point, index) => ({
    x: index,
    y: point.value1 !== undefined ? point.value1 : point.value,
    // label: point.label, // Removed to hide data point labels
    fill: point.dataPointColor || color,
    size: point.dataPointRadius || dataPointsRadius
  }));

  return (
    <View style={[styles.container, { backgroundColor }]} onLayout={handleLayout}>
      {containerWidth > 0 && (
        <VictoryChart
          theme={VictoryTheme.material}
          width={chartWidth}
          height={height}
          padding={{ left: yAxisLabelWidth + 10, top: 20, right: 20, bottom: showXAxisLabels ? 50 : 20 }}
          containerComponent={<VictoryContainer responsive={false} />}
        >
          {showYAxisLabels && (
            <VictoryAxis
              dependentAxis
              tickFormat={(t) => `${t}`}
              style={{
                axis: { stroke: semanticColors.border },
                tickLabels: { 
                  fontSize: 10, 
                  fill: semanticColors.textSecondary 
                },
                grid: showVerticalLines ? { 
                  stroke: semanticColors.border, 
                  strokeOpacity: 0.3 
                } : { stroke: 'transparent' }
              }}
              domain={maxValue ? [0, maxValue] : undefined}
            />
          )}
          
          {showXAxisLabels && (
            <VictoryAxis
              tickFormat={(t, i) => data[i]?.label || `${t}`}
              style={{
                axis: { stroke: semanticColors.border },
                tickLabels: { 
                  fontSize: 10, 
                  fill: semanticColors.textSecondary,
                  ...xAxisLabelTextStyle 
                },
                grid: { stroke: 'transparent' }
              }}
            />
          )}
          
          {showLine && (
            <VictoryLine
              data={victoryData}
              style={{
                data: { 
                  stroke: color, 
                  strokeWidth: 2 
                }
              }}
              animate={isAnimated ? {
                duration: animationDuration,
                onLoad: { duration: 500 }
              } : false}
            />
          )}
          
          <VictoryScatter
            data={victoryData}
            size={dataPointsRadius / 2}
            style={{
              data: { fill: color }
            }}
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
    paddingBottom: 25, // Espacio adicional para las etiquetas del eje X
    flex: 1,
  },
});