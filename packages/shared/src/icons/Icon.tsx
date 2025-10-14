import React from 'react';
import { IconProps, IconName } from './types';
import { svgPaths } from './svg-paths';

interface BaseIconProps {
  name: IconName;
  size?: number;
  color?: string;
  stroke?: string | undefined;
  strokeWidth?: number;
  fill?: string | undefined;
  platform?: 'web' | 'mobile'; // Nueva prop para especificar la plataforma
  className?: string; // Para web
  style?: any; // Para React Native
  testID?: string; // Para testing
}

export const Icon: React.FC<BaseIconProps> = ({ 
  name, 
  size = 24, 
  color = 'currentColor',
  stroke,
  strokeWidth = 2,
  fill = 'none',
  platform = 'web', // Por defecto web para compatibilidad
  ...props 
}) => {
  const pathData = svgPaths[name];
  
  if (!pathData) {
    console.warn(`Icon "${name}" not found`);
    return null;
  }

  // Para React Native (mobile)
  if (platform === 'mobile') {
    try {
      // Importar dinámicamente react-native-svg solo cuando sea necesario
      const { Svg, Path } = require('react-native-svg');
      
      return React.createElement(Svg, {
        width: size,
        height: size,
        viewBox: '0 0 24 24',
        fill: fill,
        ...props
      }, React.createElement(Path, {
        d: pathData,
        stroke: stroke || color,
        strokeWidth: strokeWidth,
        strokeLinecap: 'round',
        strokeLinejoin: 'round',
        fill: fill
      }));
    } catch (error) {
      // Fallback si react-native-svg no está disponible
      try {
        const { Text } = require('react-native');
        return React.createElement(Text, {
          style: {
            fontSize: size,
            color: stroke || color,
            textAlign: 'center',
            ...props.style
          },
          ...props
        }, '□'); // Símbolo simple como fallback
      } catch {
        return null;
      }
    }
  }

  // Para web, usar SVG estándar
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill={fill}
      stroke={stroke || color}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      <path d={pathData} />
    </svg>
  );
};

export default Icon;