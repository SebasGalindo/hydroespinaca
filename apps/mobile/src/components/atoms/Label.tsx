import React from 'react';
import { Text as RNText, TextStyle, View, ViewStyle } from 'react-native';
import { semanticColors, typography, spacing } from '@hidroespinaca/shared';

export interface LabelProps {
  children: React.ReactNode;
  required?: boolean;
  disabled?: boolean;
  error?: boolean;
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  style?: TextStyle;
  containerStyle?: ViewStyle;
  onPress?: () => void;
  testID?: string;
}

const labelStyles = {
  sm: { fontSize: typography.fontSize.sm, fontWeight: typography.fontWeight.medium as any },
  md: { fontSize: typography.fontSize.md, fontWeight: typography.fontWeight.medium as any },
  lg: { fontSize: typography.fontSize.lg, fontWeight: typography.fontWeight.medium as any },
} as const;

export function Label({
  children,
  required = false,
  disabled = false,
  error = false,
  size = 'md',
  color,
  style,
  containerStyle,
  onPress,
  testID,
}: LabelProps): React.ReactElement {
  const sizeStyle = labelStyles[size];
  
  const getColor = () => {
    if (color) return color;
    if (error) return semanticColors.errorText;
    if (disabled) return semanticColors.textMuted;
    return semanticColors.textSecondary;
  };
  
  const computedStyle: TextStyle = {
    ...sizeStyle,
    color: getColor(),
    marginBottom: spacing.xs,
  };

  return (
    <View style={[{ flexDirection: 'row', alignItems: 'center' }, containerStyle]}>
      <RNText
        style={[computedStyle, style]}
        onPress={onPress}
        testID={testID}
        accessibilityRole="text"
      >
        {children}
        {required && (
          <RNText style={{ color: semanticColors.errorText, marginLeft: 2 }}>
            {' *'}
          </RNText>
        )}
      </RNText>
    </View>
  );
}