import React, { useMemo } from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius, shadows, semanticColors } from '@hydroespinaca/shared';

export interface CardProps {
  children: React.ReactNode;
  variant?: 'elevated' | 'outlined' | 'filled';
  padding?: keyof typeof spacing;
  style?: ViewStyle;
  testID?: string;
}

export const Card = React.memo(function Card({
  children,
  variant = 'elevated',
  padding = 'md',
  style,
  testID,
}: CardProps): React.ReactElement {
  const variantStyle = useMemo((): ViewStyle => {
    switch (variant) {
      case 'elevated':
        return {
          backgroundColor: semanticColors.surface,
          ...shadows.sm,
          elevation: 2,
          borderWidth: 0,
        };
      case 'outlined':
        return {
          backgroundColor: semanticColors.surface,
          borderWidth: 1,
          borderColor: colors.gray[200],
        };
      case 'filled':
        return {
          backgroundColor: colors.gray[50],
          borderWidth: 0,
        };
    }
  }, [variant]);

  return (
    <View
      style={[
        styles.base,
        variantStyle,
        { padding: spacing[padding] },
        style,
      ]}
      testID={testID}
    >
      {children}
    </View>
  );
});

const styles = StyleSheet.create({
  base: {
    borderRadius: borderRadius.lg,
    overflow: 'hidden',
  },
});
