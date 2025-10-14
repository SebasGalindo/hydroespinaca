// Exportación centralizada de todos los design tokens
// Para uso en aplicaciones web y móvil

export * from './colors';
export * from './typography';
export * from './spacing';

// Re-exportación de tokens principales para fácil acceso
export { colors, semanticColors } from './colors';
export { typography, textStyles } from './typography';
export { spacing, componentSpacing, borderRadius, elevation, zIndex } from './spacing';

// Tema base que combina todos los tokens
export const baseTokens = {
  colors: {
    primary: '#166534',
    secondary: '#6b7280',
    success: '#10b981',
    warning: '#f59e0b',
    error: '#ef4444',
    info: '#3b82f6',
    background: '#ffffff',
    surface: '#ffffff',
    text: '#111827'
  },
  spacing: {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32
  },
  typography: {
    fontFamily: 'Inter',
    fontSize: {
      sm: 12,
      md: 14,
      lg: 16,
      xl: 18
    }
  },
  borderRadius: {
    sm: 4,
    md: 8,
    lg: 12
  }
};

export type BaseTokens = typeof baseTokens;