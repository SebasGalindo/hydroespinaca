import React from 'react';
import { Text, TextStyle } from 'react-native';

export interface HeadingProps {
  children: React.ReactNode;
  level?: 1 | 2 | 3 | 4 | 5 | 6;
  style?: TextStyle;
  color?: string;
}

export const Heading: React.FC<HeadingProps> = ({ 
  children, 
  level = 2, 
  style,
  color = '#2E7D32'
}) => {
  const getFontSize = (level: number): number => {
    switch (level) {
      case 1: return 32;
      case 2: return 28;
      case 3: return 24;
      case 4: return 20;
      case 5: return 18;
      case 6: return 16;
      default: return 28;
    }
  };

  const headingStyle: TextStyle = {
    fontSize: getFontSize(level),
    fontWeight: 'bold',
    color,
    marginBottom: 8,
    ...style
  };

  return <Text style={headingStyle}>{children}</Text>;
};