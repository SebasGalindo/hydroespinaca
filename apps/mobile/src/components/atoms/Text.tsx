import React from 'react';
import { Text as RNText, TextStyle, StyleSheet } from 'react-native';
import { semanticColors, typography } from '@hydroespinaca/shared';

// Text style variants
const textStyles = {
  body: { fontSize: typography.fontSize.md, lineHeight: typography.lineHeight.normal },
  caption: { fontSize: typography.fontSize.sm, lineHeight: typography.lineHeight.normal },
  label: { fontSize: typography.fontSize.sm, fontWeight: typography.fontWeight.medium, lineHeight: typography.lineHeight.normal },
  overline: { fontSize: typography.fontSize.xs, fontWeight: typography.fontWeight.semibold, lineHeight: typography.lineHeight.tight },
  h1: { fontSize: typography.fontSize['4xl'], fontWeight: typography.fontWeight.bold, lineHeight: typography.lineHeight.tight },
  h2: { fontSize: typography.fontSize['2xl'], fontWeight: typography.fontWeight.bold, lineHeight: typography.lineHeight.tight },
  h3: { fontSize: typography.fontSize.xl, fontWeight: typography.fontWeight.semibold, lineHeight: typography.lineHeight.normal },
} as const;

export interface TextProps {
  children: React.ReactNode;
  variant?: keyof typeof textStyles;
  size?: keyof typeof typography.fontSize;
  weight?: keyof typeof typography.fontWeight;
  color?: string;
  align?: 'left' | 'center' | 'right' | 'justify';
  numberOfLines?: number;
  ellipsizeMode?: 'head' | 'middle' | 'tail' | 'clip';
  style?: TextStyle;
  onPress?: () => void;
  testID?: string;
}

export function Text({
  children,
  variant = 'body',
  size,
  weight,
  color = semanticColors.textPrimary,
  align = 'left',
  numberOfLines,
  ellipsizeMode = 'tail',
  style,
  onPress,
  testID,
}: TextProps): React.ReactElement {
  const textStyle = textStyles[variant];

  // Determine the effective font size that will be applied
  const variantFontSize = (textStyle as any)?.fontSize ?? typography.fontSize.base;
  const effectiveFontSize = size ? typography.fontSize[size] : variantFontSize;
  
  const computedStyle: TextStyle = {
    ...textStyle,
    color,
    textAlign: align,
    ...(size && { fontSize: typography.fontSize[size] }),
    ...(weight && { fontWeight: (typography.fontWeight as any)[weight] || (weight as any) }),
  };

  // Normalize lineHeight tokens: convert ratio (e.g., 1.5) to RN pixels using effective font size
  const lh = (textStyle as any)?.lineHeight;
  if (typeof lh === 'number' && lh > 0 && lh < 10) {
    computedStyle.lineHeight = Math.round(effectiveFontSize * lh);
  }

  return (
    <RNText
      style={[computedStyle, style]}
      numberOfLines={numberOfLines}
      ellipsizeMode={ellipsizeMode}
      onPress={onPress}
      testID={testID}
    >
      {children}
    </RNText>
  );
}