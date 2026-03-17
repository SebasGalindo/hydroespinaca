import React, { useMemo } from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, typography, borderRadius } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface BadgeProps {
  children: React.ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
  size?: 'sm' | 'md';
  style?: ViewStyle;
  testID?: string;
}

const variantStyles = {
  default: {
    backgroundColor: colors.gray[100],
    textColor: colors.gray[700],
  },
  success: {
    backgroundColor: colors.hidro[100],
    textColor: colors.hidro[700],
  },
  warning: {
    backgroundColor: colors.warning[100],
    textColor: colors.warning[800],
  },
  error: {
    backgroundColor: colors.error[100],
    textColor: colors.error[700],
  },
  info: {
    backgroundColor: colors.info[100],
    textColor: colors.info[700],
  },
} as const;

const sizeStyles = {
  sm: {
    paddingVertical: 1,
    paddingHorizontal: spacing.xs,
    fontSize: typography.fontSize.xs as number,
  },
  md: {
    paddingVertical: 2,
    paddingHorizontal: spacing.sm,
    fontSize: typography.fontSize.sm as number,
  },
} as const;

export const Badge = React.memo(function Badge({
  children,
  variant = 'default',
  size = 'sm',
  style,
  testID,
}: BadgeProps): React.ReactElement {
  const variantStyle = variantStyles[variant];
  const sizeStyle = sizeStyles[size];

  const textStyle = useMemo(
    () => ({ fontSize: sizeStyle.fontSize, fontWeight: typography.fontWeight.semibold as any }),
    [sizeStyle.fontSize],
  );

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: variantStyle.backgroundColor,
          paddingVertical: sizeStyle.paddingVertical,
          paddingHorizontal: sizeStyle.paddingHorizontal,
        },
        style,
      ]}
      testID={testID}
      accessibilityRole="text"
    >
      <Text
        variant="caption"
        color={variantStyle.textColor}
        style={textStyle}
      >
        {children}
      </Text>
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    alignSelf: 'flex-start',
    borderRadius: borderRadius.xl,
    overflow: 'hidden',
  },
});
