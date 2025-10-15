// Design tokens de colores extraídos de la aplicación web
// Basado en globals.css y shared/styles/colors.ts

export const colors = {
  // Paleta principal HydroEspinaca
  primary: {
    50: '#f0fdf4',
    100: '#dcfce7',
    500: '#22c55e',
    600: '#16a34a',
    700: '#15803d',
    800: '#166534',  // hidro-green-primary
    900: '#14532d'
  },
  
  // Grises del sistema
  gray: {
    50: '#f9fafb',
    100: '#f3f4f6',
    200: '#e5e7eb',
    300: '#d1d5db',
    400: '#9ca3af',
    500: '#6b7280',  // hidro-gray
    600: '#4b5563',
    700: '#374151',
    800: '#1f2937',
    900: '#111827'
  },
  
  // Estados de feedback
  success: '#10b981',
  warning: '#f59e0b',
  error: '#ef4444',
  info: '#3b82f6',
  
  // Colores específicos Hidro
  hidro: {
    primary: '#166534',
    light: '#BEEEBE',
    bg: '#dcfce7',
    bgLight: '#f0fdf4',
    bgPale: '#f8fffe', // Verde pálido muy suave para fondos
    hover: '#A8E6A8'
  },
  
  // Colores base
  white: '#ffffff',
  black: '#000000',
  transparent: 'transparent',
  
  // Overlay
  overlay: 'rgba(0, 0, 0, 0.5)'
};

// Colores semánticos para uso en componentes
export const semanticColors = {
  // Texto
  textPrimary: colors.gray[900],
  textSecondary: colors.gray[600],
  textMuted: colors.gray[500],
  textInverse: colors.white,
  textPlaceholder: colors.gray[400],
  textDisabled: colors.gray[400],
  
  // Fondos
  background: colors.white,
  backgroundSecondary: colors.gray[50],
  backgroundMuted: colors.gray[100],
  backgroundPrimary: colors.white, // Cambiado de colors.primary[600] a colors.white para mejor accesibilidad
  
  // Superficies
  surface: colors.white,
  surfaceElevated: colors.white,
  
  // Bordes
  border: colors.gray[200],
  borderMuted: colors.gray[100],
  borderFocus: colors.primary[600],
  borderDisabled: colors.gray[300],
  
  // Colores primarios
  primary: colors.primary[600],
  
  // Estados
  successText: colors.primary[600],
  successBg: colors.primary[50],
  warningText: colors.warning,
  warningBg: '#fef3c7',
  errorText: colors.error,
  errorBg: '#fef2f2',
  infoText: colors.info,
  infoBg: '#eff6ff',
  destructiveText: colors.error,
  destructiveBg: '#fef2f2'
};

export type ColorToken = keyof typeof colors;
export type SemanticColorToken = keyof typeof semanticColors;