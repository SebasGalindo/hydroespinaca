import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing } from '@hydroespinaca/shared';

export interface DividerProps {
  orientation?: 'horizontal' | 'vertical';
  color?: string;
  thickness?: number;
  spacing?: number;
  style?: ViewStyle;
  testID?: string;
}

export function Divider({
  orientation = 'horizontal',
  color = colors.gray[200],
  thickness = StyleSheet.hairlineWidth,
  spacing: spacingProp,
  style,
  testID,
}: DividerProps): React.ReactElement {
  const isHorizontal = orientation === 'horizontal';
  const marginKey = isHorizontal ? 'marginVertical' : 'marginHorizontal';
  const marginValue = spacingProp ?? spacing.sm;

  return (
    <View
      style={[
        isHorizontal
          ? { height: thickness, width: '100%', backgroundColor: color }
          : { width: thickness, height: '100%', backgroundColor: color },
        { [marginKey]: marginValue },
        style,
      ]}
      testID={testID}
      accessibilityRole="none"
    />
  );
}
