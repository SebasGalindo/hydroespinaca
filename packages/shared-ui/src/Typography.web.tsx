import React from 'react';

export interface TypographyProps {
  children: React.ReactNode;
  variant?: 'body' | 'caption' | 'subtitle' | 'overline';
  style?: React.CSSProperties;
  color?: string;
}

export const Typography: React.FC<TypographyProps> = ({ 
  children, 
  variant = 'body', 
  style,
  color = '#333'
}) => {
  const getVariantStyle = (variant: string): React.CSSProperties => {
    switch (variant) {
      case 'body':
        return { fontSize: '16px', lineHeight: '24px' };
      case 'caption':
        return { fontSize: '12px', lineHeight: '16px' };
      case 'subtitle':
        return { fontSize: '14px', lineHeight: '20px', fontWeight: 500 };
      case 'overline':
        return { fontSize: '10px', lineHeight: '16px', textTransform: 'uppercase' };
      default:
        return { fontSize: '16px', lineHeight: '24px' };
    }
  };

  const textStyle: React.CSSProperties = {
    color,
    margin: 0,
    ...getVariantStyle(variant),
    ...style
  };

  return <p style={textStyle}>{children}</p>;
};