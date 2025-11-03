/**
 * Spacing tokens for consistent layout and spacing
 * Based on 4px base unit for React Native
 */

export const spacing = {
  // Base unit: 4px
  none: 0,
  xxs: 2,   // 0.125rem equivalent
  xs: 4,    // 0.25rem equivalent
  sm: 8,    // 0.5rem equivalent
  md: 12,   // 0.75rem equivalent
  lg: 16,   // 1rem equivalent
  xl: 20,   // 1.25rem equivalent
  '2xl': 24, // 1.5rem equivalent
  '3xl': 32, // 2rem equivalent
  '4xl': 40, // 2.5rem equivalent
  '5xl': 48, // 3rem equivalent
  '6xl': 64, // 4rem equivalent
  '7xl': 80, // 5rem equivalent
  '8xl': 96, // 6rem equivalent
} as const;

// Export type for TypeScript
export type Spacing = keyof typeof spacing;
export type SpacingValue = (typeof spacing)[Spacing];
