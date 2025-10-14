import React, { useState } from 'react';
import { View, StyleSheet, LayoutChangeEvent } from 'react-native';
import { VictoryChart, VictoryLine, VictoryAxis, VictoryTheme, VictoryContainer, VictoryScatter } from 'victory-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';

export interface LineChartDataPoint {
  value: number;
  label?: string;
  dataPointText?: string;
  labelTextStyle?: object;
  dataPointColor?: string;
}

export interface LineChartProps {
  data: LineChartDataPoint[];
  height?: number;
  color?: string;
  thickness?: number;
  curved?: boolean;
  showDataPoints?: boolean;
  showVerticalLines?: boolean;
  showXAxisLabels?: boolean;
  showYAxisLabels?: boolean;
  yAxisLabelWidth?: number;
  xAxisLabelTextStyle?: object;
  yAxisLabelTextStyle?: object;
  animateOnDataChange?: boolean;
  animationDuration?: number;
  maxValue?: number;
  stepValue?: number;
  noOfSections?: number;
  backgroundColor?: string;
  isAnimated?: boolean;
}

export function LineChart({
  data,
  height = 200,
  color = semanticColors.primary,
  thickness = 2,
  curved = true,
  showDataPoints = true,
  showVerticalLines = false,
  showXAxisLabels = true,
  showYAxisLabels = true,
  yAxisLabelWidth = 40,
  xAxisLabelTextStyle,
  yAxisLabelTextStyle,
  animateOnDataChange = true,
  animationDuration = 1000,
  maxValue,
  stepValue,
  noOfSections = 5,
  backgroundColor = 'transparent',
  isAnimated = true,
}: LineChartProps): React.ReactElement {
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
    y: point.value
    // label: point.label // Removed to hide data point labels
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
          
          <VictoryLine
            data={victoryData}
            style={{
              data: { 
                stroke: color, 
                strokeWidth: thickness 
              }
            }}
            interpolation={curved ? "cardinal" : "linear"}
            animate={isAnimated ? {
              duration: animationDuration,
              onLoad: { duration: 500 }
            } : false}
          />
          
          {showDataPoints && (
            <VictoryScatter
              data={victoryData}
              size={2}
              style={{
                data: { fill: color }
              }}
              animate={isAnimated ? {
                duration: animationDuration,
                onLoad: { duration: 500 }
              } : false}
            />
          )}
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