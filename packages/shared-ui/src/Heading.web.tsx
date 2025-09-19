import React from 'react';

export interface HeadingProps {
  children: React.ReactNode;
  level?: 1 | 2 | 3 | 4 | 5 | 6;
  style?: React.CSSProperties;
  color?: string;
}

export const Heading: React.FC<HeadingProps> = ({ 
  children, 
  level = 2, 
  style,
  color = '#2E7D32'
}) => {
  const Tag = `h${level}` as React.ElementType;
  
  const headingStyle: React.CSSProperties = {
    color,
    marginBottom: '8px',
    fontWeight: 'bold',
    ...style
  };

  return <Tag style={headingStyle}>{children}</Tag>;
};