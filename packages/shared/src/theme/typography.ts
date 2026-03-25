/**
 * Typography tokens for consistent text styling
 * Font sizes, weights, and line heights
 */

export const typography = {
  // Font families
  fontFamily: {
    // System font stack for React Native
    system: 'System',
    primary: 'System', // Alias for system
    // For web, you can override with your preferred fonts
    sans: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
    mono: 'Menlo, Monaco, "Courier New", monospace',
  },

  // Font sizes (in pixels for React Native)
  fontSize: {
    micro: 9,  // Chart tooltips, tiny labels
    xxs: 10,   // Small chart labels, badges
    xs: 12,   // 0.75rem equivalent
    sm: 14,   // 0.875rem equivalent
    base: 16,  // Base font size
    md: 16,   // 1rem equivalent (base)
    lg: 18,   // 1.125rem equivalent
    xl: 20,   // 1.25rem equivalent
    '2xl': 24, // 1.5rem equivalent
    '3xl': 30, // 1.875rem equivalent
    '4xl': 36, // 2.25rem equivalent
    '5xl': 48, // 3rem equivalent
    '6xl': 60, // 3.75rem equivalent
  },

  // Font weights
  fontWeight: {
    light: '300' as const,
    normal: '400' as const,
    medium: '500' as const,
    semibold: '600' as const,
    bold: '700' as const,
    extrabold: '800' as const,
  },

  // Line heights (multipliers)
  lineHeight: {
    none: 1,
    tight: 1.25,
    normal: 1.5,
    relaxed: 1.75,
    loose: 2,
  },

  // Letter spacing (for React Native)
  letterSpacing: {
    tighter: -0.5,
    tight: -0.25,
    normal: 0,
    wide: 0.25,
    wider: 0.5,
    widest: 1,
  },
} as const;

// Export types for TypeScript
export type FontFamily = keyof typeof typography.fontFamily;
export type FontSize = keyof typeof typography.fontSize;
export type FontWeight = keyof typeof typography.fontWeight;
export type LineHeight = keyof typeof typography.lineHeight;
export type LetterSpacing = keyof typeof typography.letterSpacing;
