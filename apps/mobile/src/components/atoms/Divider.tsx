import React from 'react';
import { View, ViewStyle } from 'react-native';
import { semanticColors } from '@hydroespinaca/shared';

export interface DividerProps {
  orientation?: 'horizontal' | 'vertical';
  thickness?: number;
  color?: string;
  style?: ViewStyle;
  testID?: string;
}

export function Divider({
  orientation = 'horizontal',
  thickness = 1,
  color = semanticColors.border,
  style,
  testID,
}: DividerProps): React.ReactElement {
  const dividerStyle: ViewStyle = {
    backgroundColor: color,
    ...(orientation === 'horizontal' 
      ? { height: thickness, width: '100%' }
      : { width: thickness, height: '100%' }
    ),
  };

  return (
    <View 
      style={[dividerStyle, style]} 
      testID={testID}
      accessible={true}
      accessibilityRole="none"
    />
  );
}