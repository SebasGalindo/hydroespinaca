import React from 'react';
import { Text as RNText, TextStyle } from 'react-native';

export interface TypographyProps {
  children: React.ReactNode;
  variant?: 'body' | 'caption' | 'subtitle' | 'overline';
  style?: TextStyle;
  color?: string;
}

export const Typography: React.FC<TypographyProps> = ({ 
  children, 
  variant = 'body', 
  style,
  color = '#333'
}) => {
  const getVariantStyle = (variant: string): TextStyle => {
    switch (variant) {
      case 'body':
        return { fontSize: 16, lineHeight: 24 };
      case 'caption':
        return { fontSize: 12, lineHeight: 16 };
      case 'subtitle':
        return { fontSize: 14, lineHeight: 20, fontWeight: '500' };
      case 'overline':
        return { fontSize: 10, lineHeight: 16, textTransform: 'uppercase' };
      default:
        return { fontSize: 16, lineHeight: 24 };
    }
  };

  const textStyle: TextStyle = {
    color,
    ...getVariantStyle(variant),
    ...style
  };

  return <RNText style={textStyle}>{children}</RNText>;
};