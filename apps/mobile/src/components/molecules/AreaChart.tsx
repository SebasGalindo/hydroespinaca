import React, { useState } from 'react';
import { View, StyleSheet, LayoutChangeEvent } from 'react-native';
import { VictoryChart, VictoryArea, VictoryAxis, VictoryTheme, VictoryContainer } from 'victory-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';

export interface AreaChartDataPoint {
  value: number;
  label?: string;
  dataPointText?: string;
  labelTextStyle?: object;
  dataPointColor?: string;
}

export interface AreaChartProps {
  data: AreaChartDataPoint[];
  height?: number;
  color?: string;
  fillOpacity?: number;
  showXAxisLabels?: boolean;
  showYAxisLabels?: boolean;
  yAxisLabelWidth?: number;
  xAxisLabelTextStyle?: object;
  yAxisLabelTextStyle?: object;
  animationDuration?: number;
  maxValue?: number;
  backgroundColor?: string;
  isAnimated?: boolean;
  interpolation?: 'basis' | 'cardinal' | 'catmullRom' | 'linear' | 'monotoneX' | 'monotoneY' | 'natural' | 'step' | 'stepAfter' | 'stepBefore';
}

export function AreaChart({
  data,
  height = 200,
  color = semanticColors.primary,
  fillOpacity = 0.3,
  showXAxisLabels = true,
  showYAxisLabels = true,
  yAxisLabelWidth = 40,
  xAxisLabelTextStyle,
  yAxisLabelTextStyle,
  animationDuration = 1000,
  maxValue,
  backgroundColor = 'transparent',
  isAnimated = true,
  interpolation = 'cardinal',
}: AreaChartProps): React.ReactElement {
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
    y: point.value,
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
                  fill: semanticColors.textSecondary,
                  ...yAxisLabelTextStyle 
                },
                grid: { 
                  stroke: semanticColors.border, 
                  strokeOpacity: 0.3 
                }
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
          
          <VictoryArea
            data={victoryData}
            style={{
              data: { 
                fill: color,
                fillOpacity: fillOpacity,
                stroke: color,
                strokeWidth: 2,
              }
            }}
            interpolation={interpolation}
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
