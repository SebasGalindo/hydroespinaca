/**
 * Border radius tokens for consistent rounded corners
 */

export const borderRadius = {
  none: 0,
  xs: 2,
  sm: 4,
  md: 6,
  lg: 8,
  xl: 12,
  '2xl': 16,
  '3xl': 24,
  '4xl': 32,  // Pill badges, large rounded
  full: 9999, // Fully rounded (circle/pill)
} as const;

// Export type for TypeScript
export type BorderRadius = keyof typeof borderRadius;
export type BorderRadiusValue = (typeof borderRadius)[BorderRadius];
