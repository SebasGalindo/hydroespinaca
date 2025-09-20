import React from 'react';

export interface ContainerProps {
  children: React.ReactNode;
  style?: React.CSSProperties;
  scrollable?: boolean;
  padding?: number | 'none' | 'sm' | 'md' | 'lg' | 'xl';
  backgroundColor?: string;
}

export const Container: React.FC<ContainerProps> = ({ 
  children, 
  style,
  scrollable = false,
  padding = 'md',
  backgroundColor = 'transparent'
}) => {
  const getPaddingValue = (padding: ContainerProps['padding']): string => {
    if (typeof padding === 'number') return `${padding}px`;
    switch (padding) {
      case 'none': return '0';
      case 'sm': return '8px';
      case 'md': return '16px';
      case 'lg': return '24px';
      case 'xl': return '32px';
      default: return '16px';
    }
  };

  const containerStyle: React.CSSProperties = {
    backgroundColor,
    padding: getPaddingValue(padding),
    ...(scrollable && { 
      overflowY: 'auto',
      maxHeight: '100vh' 
    }),
    ...style
  };

  return <div style={containerStyle}>{children}</div>;
};