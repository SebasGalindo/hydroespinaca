import React from 'react';
import { Text as RNText, TextStyle, StyleSheet } from 'react-native';
import { semanticColors, typography, textStyles } from '@hidroespinaca/shared';

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