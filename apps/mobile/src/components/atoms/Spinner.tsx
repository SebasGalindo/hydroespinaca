import React from 'react';
import { ActivityIndicator, ViewStyle } from 'react-native';
import { semanticColors } from '@hidroespinaca/shared';

export interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg' | number;
  color?: string;
  animating?: boolean;
  style?: ViewStyle;
  testID?: string;
}

const sizeStyles = {
  sm: 16,
  md: 24,
  lg: 32,
} as const;

export function Spinner({
  size = 'md',
  color = semanticColors.primary,
  animating = true,
  style,
  testID,
}: SpinnerProps): React.ReactElement {
  const spinnerSize = typeof size === 'number' ? size : sizeStyles[size];

  return (
    <ActivityIndicator
      size={spinnerSize}
      color={color}
      animating={animating}
      style={style}
      testID={testID}
      hidesWhenStopped
    />
  );
}