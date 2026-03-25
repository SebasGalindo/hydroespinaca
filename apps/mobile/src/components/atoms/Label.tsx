import React, { useMemo } from 'react';
import { Text as RNText, TextStyle, View, ViewStyle, StyleSheet } from 'react-native';
import { semanticColors, typography, spacing } from '@hydroespinaca/shared';

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

export const Label = React.memo(function Label({
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

  const resolvedColor = useMemo(() => {
    if (color) return color;
    if (error) return semanticColors.errorText;
    if (disabled) return semanticColors.textMuted;
    return semanticColors.textSecondary;
  }, [color, error, disabled]);

  const computedStyle = useMemo<TextStyle>(
    () => ({
      ...sizeStyle,
      color: resolvedColor,
      marginBottom: spacing.xs,
    }),
    [sizeStyle, resolvedColor],
  );

  return (
    <View style={[labelContainerStyles.row, containerStyle]}>
      <RNText
        style={[computedStyle, style]}
        onPress={onPress}
        testID={testID}
        accessibilityRole="text"
      >
        {children}
        {required && (
          <RNText style={labelContainerStyles.asterisk}>
            {' *'}
          </RNText>
        )}
      </RNText>
    </View>
  );
});

const labelContainerStyles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  asterisk: {
    color: semanticColors.errorText,
    marginLeft: 2,
  },
});