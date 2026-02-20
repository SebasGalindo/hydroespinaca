/**
 * Color palette for HydroEspinaca
 * Base colors and semantic color tokens for consistent theming
 */

// Base color palette
export const colors = {
  // Primary colors - Green theme for hydroponic system
  primary: {
    50: '#f0fdf4',
    100: '#dcfce7',
    200: '#bbf7d0',
    300: '#86efac',
    400: '#4ade80',
    500: '#22c55e',
    600: '#16a34a',
    700: '#15803d',
    800: '#166534',
    900: '#14532d',
  },

  // Gray scale
  gray: {
    50: '#f9fafb',
    100: '#f3f4f6',
    200: '#e5e7eb',
    300: '#d1d5db',
    400: '#9ca3af',
    500: '#6b7280',
    600: '#4b5563',
    700: '#374151',
    800: '#1f2937',
    900: '#111827',
  },

  // Error/Danger colors
  error: {
    50: '#fef2f2',
    100: '#fee2e2',
    200: '#fecaca',
    300: '#fca5a5',
    400: '#f87171',
    500: '#ef4444',
    600: '#dc2626',
    700: '#b91c1c',
    800: '#991b1b',
    900: '#7f1d1d',
  },

  // Warning colors
  warning: {
    50: '#fffbeb',
    100: '#fef3c7',
    200: '#fde68a',
    300: '#fcd34d',
    400: '#fbbf24',
    500: '#f59e0b',
    600: '#d97706',
    700: '#b45309',
    800: '#92400e',
    900: '#78350f',
  },

  // Success colors (using primary green)
  success: {
    50: '#f0fdf4',
    100: '#dcfce7',
    200: '#bbf7d0',
    300: '#86efac',
    400: '#4ade80',
    500: '#22c55e',
    600: '#16a34a',
    700: '#15803d',
    800: '#166534',
    900: '#14532d',
  },

  // Info colors
  info: {
    50: '#eff6ff',
    100: '#dbeafe',
    200: '#bfdbfe',
    300: '#93c5fd',
    400: '#60a5fa',
    500: '#3b82f6',
    600: '#2563eb',
    700: '#1d4ed8',
    800: '#1e40af',
    900: '#1e3a8a',
  },

  // Pure colors
  white: '#ffffff',
  black: '#000000',
  transparent: 'transparent',

  // HydroEspinaca brand color (specific green for hydroponic branding)
  hidro: {
    50: '#f0fdf4',
    100: '#dcfce7',
    200: '#bbf7d0',
    300: '#86efac',
    400: '#4ade80',
    500: '#22c55e',
    600: '#16a34a',
    700: '#15803d',
    800: '#166534',
    900: '#14532d',
  },
};

// Semantic colors - Map base colors to semantic meanings
export const semanticColors = {
  // Primary brand color
  primary: colors.primary[700],
  primaryLight: colors.primary[500],
  primaryDark: colors.primary[800],

  // Backgrounds
  background: colors.white,
  backgroundPrimary: colors.white,
  backgroundSecondary: colors.gray[50],
  backgroundTertiary: colors.gray[100],
  backgroundMuted: colors.gray[100],

  // Surfaces (cards, panels)
  surface: colors.white,
  surfaceSecondary: colors.gray[50],

  // Text colors
  textPrimary: colors.gray[900],
  textSecondary: colors.gray[600],
  textTertiary: colors.gray[500],
  textMuted: colors.gray[500],
  textPlaceholder: colors.gray[400],
  textInverse: colors.white,
  textDisabled: colors.gray[400],

  // Border colors
  border: colors.gray[200],
  borderLight: colors.gray[100],
  borderDark: colors.gray[300],
  borderMuted: colors.gray[200],
  borderFocus: colors.primary[500],

  // Status colors - Success
  success: colors.success[600],
  successBg: colors.success[50],
  successBorder: colors.success[200],
  successText: colors.success[700],

  // Status colors - Warning
  warning: colors.warning[500],
  warningBg: colors.warning[50],
  warningBorder: colors.warning[200],
  warningText: colors.warning[700],

  // Status colors - Error
  error: colors.error[600],
  errorBg: colors.error[50],
  errorBorder: colors.error[200],
  errorText: colors.error[700],

  // Status colors - Info
  info: colors.info[500],
  infoBg: colors.info[50],
  infoBorder: colors.info[200],
  infoText: colors.info[700],

  // Interactive elements
  link: colors.primary[700],
  linkHover: colors.primary[800],
  linkVisited: colors.primary[900],

  // Form elements
  inputBackground: colors.white,
  inputBorder: colors.gray[300],
  inputBorderFocus: colors.primary[500],
  inputBorderError: colors.error[500],
  inputText: colors.gray[900],
  inputPlaceholder: colors.gray[400],
  inputDisabled: colors.gray[100],

  // Status colors - Light backgrounds (for badges, indicators)
  successLight: colors.success[100],   // #dcfce7
  warningLight: colors.warning[100],   // #fef3c7
  errorLight: colors.error[100],       // #fee2e2
  infoLight: colors.info[100],         // #dbeafe

  // Extended warning colors
  warningDark: colors.warning[800],    // #92400e
  warningBgLight: '#fff7ed',           // orange-50 for banners
  warningBorderLight: '#fed7aa',       // orange-200 for banners

  // Extended text/icon colors
  warningIcon: '#a16207',              // amber-700 for dark warning icons
  dangerIcon: '#c2410c',               // orange-700 for disconnection states

  // Overlay colors
  overlay: 'rgba(0, 0, 0, 0.5)',
  overlayLight: 'rgba(0, 0, 0, 0.25)',
  overlayMedium: 'rgba(0, 0, 0, 0.4)',
  overlayDark: 'rgba(0, 0, 0, 0.75)',
  overlayStrong: 'rgba(0, 0, 0, 0.8)',
  overlayWhite: 'rgba(255, 255, 255, 0.7)',
  overlayWhiteStrong: 'rgba(255, 255, 255, 0.8)',

  // Shadow colors
  shadow: 'rgba(0, 0, 0, 0.1)',
  shadowMedium: 'rgba(0, 0, 0, 0.15)',
  shadowStrong: 'rgba(0, 0, 0, 0.25)',
};

// Chart color palette for data visualization
export const chartColors = {
  blue: '#3b82f6',
  red: '#ef4444',
  green: '#10b981',
  amber: '#f59e0b',
  cyan: '#06b6d4',
  violet: '#8b5cf6',
  pink: '#ec4899',
  teal: '#14b8a6',
  orange: '#f97316',
  indigo: '#6366f1',
  lime: '#84cc16',
  rose: '#f43f5e',
} as const;

// Ordered palette array for indexed access
export const chartColorPalette = [
  chartColors.blue,
  chartColors.red,
  chartColors.green,
  chartColors.amber,
  chartColors.cyan,
  chartColors.violet,
  chartColors.pink,
  chartColors.teal,
  chartColors.orange,
  chartColors.indigo,
  chartColors.lime,
  chartColors.rose,
] as const;

// Export types for TypeScript
export type ColorScale = typeof colors.primary;
export type SemanticColor = keyof typeof semanticColors;
