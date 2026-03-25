import React, { useMemo } from 'react';
import { Text as RNText, TextStyle } from 'react-native';
import { semanticColors, typography } from '@hydroespinaca/shared';

export interface HeadingProps {
  children: React.ReactNode;
  level?: 1 | 2 | 3 | 4 | 5 | 6;
  color?: string;
  align?: 'left' | 'center' | 'right' | 'justify';
  numberOfLines?: number;
  ellipsizeMode?: 'head' | 'middle' | 'tail' | 'clip';
  style?: TextStyle;
  onPress?: () => void;
  testID?: string;
}

const headingStyles = {
  1: { fontSize: typography.fontSize['5xl'], fontWeight: typography.fontWeight.bold as any },
  2: { fontSize: typography.fontSize['4xl'], fontWeight: typography.fontWeight.bold as any },
  3: { fontSize: typography.fontSize['3xl'], fontWeight: typography.fontWeight.semibold as any },
  4: { fontSize: typography.fontSize['2xl'], fontWeight: typography.fontWeight.semibold as any },
  5: { fontSize: typography.fontSize.xl, fontWeight: typography.fontWeight.medium as any },
  6: { fontSize: typography.fontSize.lg, fontWeight: typography.fontWeight.medium as any },
} as const;

export const Heading = React.memo(function Heading({
  children,
  level = 1,
  color = semanticColors.textPrimary,
  align = 'left',
  numberOfLines,
  ellipsizeMode = 'tail',
  style,
  onPress,
  testID,
}: HeadingProps): React.ReactElement {
  const headingStyle = headingStyles[level];

  const computedStyle = useMemo<TextStyle>(
    () => ({
      ...headingStyle,
      color,
      textAlign: align,
      marginBottom: typography.lineHeight.tight,
    }),
    [headingStyle, color, align],
  );

  return (
    <RNText
      style={[computedStyle, style]}
      numberOfLines={numberOfLines}
      ellipsizeMode={ellipsizeMode}
      onPress={onPress}
      testID={testID}
      accessibilityRole="header"
    >
      {children}
    </RNText>
  );
});