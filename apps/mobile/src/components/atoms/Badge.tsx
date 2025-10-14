import React from 'react';
import { View, ViewStyle, TextStyle } from 'react-native';
import { semanticColors, typography, borderRadius } from '@hidroespinaca/shared';
import { Text } from './Text';

export interface BadgeProps {
  count?: number;
  variant?: 'default' | 'primary' | 'success' | 'warning' | 'error' | 'info';
  size?: 'sm' | 'md' | 'lg';
  showZero?: boolean;
  maxCount?: number;
  dot?: boolean;
  children?: React.ReactNode;
  style?: ViewStyle;
  testID?: string;
}

const sizeStyles = {
  sm: { 
    minWidth: 16, 
    height: 16, 
    paddingHorizontal: 4,
    fontSize: typography.fontSize.xs,
    dotSize: 6
  },
  md: { 
    minWidth: 20, 
    height: 20, 
    paddingHorizontal: 6,
    fontSize: typography.fontSize.sm,
    dotSize: 8
  },
  lg: { 
    minWidth: 24, 
    height: 24, 
    paddingHorizontal: 8,
    fontSize: typography.fontSize.md,
    dotSize: 10
  },
} as const;

const variantStyles = {
  default: {
    backgroundColor: semanticColors.backgroundSecondary,
    color: semanticColors.textPrimary,
  },
  primary: {
    backgroundColor: semanticColors.primary,
    color: semanticColors.textInverse,
  },
  success: {
    backgroundColor: semanticColors.successBg,
    color: semanticColors.successText,
  },
  warning: {
    backgroundColor: semanticColors.warningBg,
    color: semanticColors.warningText,
  },
  error: {
    backgroundColor: semanticColors.errorBg,
    color: semanticColors.errorText,
  },
  info: {
    backgroundColor: semanticColors.infoBg,
    color: semanticColors.infoText,
  },
} as const;

export function Badge({
  count = 0,
  variant = 'default',
  size = 'md',
  showZero = false,
  maxCount = 99,
  dot = false,
  children,
  style,
  testID,
}: BadgeProps): React.ReactElement | null {
  const sizeStyle = sizeStyles[size];
  const variantStyle = variantStyles[variant];
  
  // No mostrar badge si count es 0 y showZero es false
  if (!dot && count === 0 && !showZero && !children) {
    return null;
  }
  
  const displayCount = count > maxCount ? `${maxCount}+` : count.toString();
  
  const getBadgeStyle = (): ViewStyle => ({
    minWidth: dot ? sizeStyle.dotSize : sizeStyle.minWidth,
    height: dot ? sizeStyle.dotSize : sizeStyle.height,
    paddingHorizontal: dot ? 0 : sizeStyle.paddingHorizontal,
    backgroundColor: variantStyle.backgroundColor,
    borderRadius: dot ? sizeStyle.dotSize / 2 : sizeStyle.height / 2,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'flex-start',
  });
  
  const getTextStyle = (): TextStyle => ({
    fontSize: sizeStyle.fontSize,
    color: variantStyle.color,
    fontWeight: typography.fontWeight.semibold as any,
    lineHeight: sizeStyle.fontSize * 1.2,
  });
  
  return (
    <View
      style={[getBadgeStyle(), style]}
      testID={testID}
      accessibilityRole="text"
      accessibilityLabel={
        dot 
          ? `${variant} indicator` 
          : children 
            ? `Badge: ${children}` 
            : `Count: ${displayCount}`
      }
    >
      {!dot && (
        <Text style={getTextStyle()}>
          {children || displayCount}
        </Text>
      )}
    </View>
  );
}