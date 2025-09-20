import React from 'react';
import { View, ViewStyle, ScrollView } from 'react-native';

export interface ContainerProps {
  children: React.ReactNode;
  style?: ViewStyle;
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
  const getPaddingValue = (padding: ContainerProps['padding']): number => {
    if (typeof padding === 'number') return padding;
    switch (padding) {
      case 'none': return 0;
      case 'sm': return 8;
      case 'md': return 16;
      case 'lg': return 24;
      case 'xl': return 32;
      default: return 16;
    }
  };

  const containerStyle: ViewStyle = {
    backgroundColor,
    padding: getPaddingValue(padding),
    ...style
  };

  if (scrollable) {
    return (
      <ScrollView style={containerStyle} contentContainerStyle={{ flexGrow: 1 }}>
        {children}
      </ScrollView>
    );
  }

  return <View style={containerStyle}>{children}</View>;
};