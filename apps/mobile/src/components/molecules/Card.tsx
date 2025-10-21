import React from 'react';
import { View, TouchableOpacity, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius } from '@hydroespinaca/shared';

export interface CardProps {
  children: React.ReactNode;
  padding?: 'none' | 'sm' | 'md' | 'lg';
  elevated?: boolean;
  onPress?: () => void;
  fullWidth?: boolean;
  style?: ViewStyle;
  testID?: string;
}

const paddingMap = {
  none: 0,
  sm: spacing.sm,
  md: spacing.md,
  lg: spacing.lg,
} as const;

export function Card({
  children,
  padding = 'md',
  elevated = false,
  onPress,
  fullWidth = true,
  style,
  testID,
}: CardProps): React.ReactElement {
  const baseStyle: ViewStyle = {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: semanticColors.border,
    padding: paddingMap[padding],
    width: fullWidth ? '100%' : undefined,
  };

  const elevationStyle: ViewStyle = elevated
    ? {
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.15,
        shadowRadius: 4,
        elevation: 3,
      }
    : {};

  const content = (
    <View style={[baseStyle, elevationStyle, style]} testID={testID}>
      {children}
    </View>
  );

  if (onPress) {
    return (
      <TouchableOpacity onPress={onPress} activeOpacity={0.9}>
        {content}
      </TouchableOpacity>
    );
  }

  return content;
}