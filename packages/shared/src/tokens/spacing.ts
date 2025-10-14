// Design tokens de espaciado extraídos de la aplicación web
// Sistema basado en múltiplos de 4px (escala 4/8)

export const spacing = {
  // Espaciado base (múltiplos de 4px)
  xs: 4,     // 0.25rem
  sm: 8,     // 0.5rem
  md: 12,    // 0.75rem
  lg: 16,    // 1rem
  xl: 20,    // 1.25rem
  '2xl': 24, // 1.5rem
  '3xl': 32, // 2rem
  '4xl': 40, // 2.5rem
  '5xl': 48, // 3rem
  '6xl': 64, // 4rem
  '7xl': 80, // 5rem
  '8xl': 96  // 6rem
};

// Espaciado específico para componentes
export const componentSpacing = {
  // Padding interno de componentes
  buttonPadding: {
    sm: { horizontal: spacing.md, vertical: spacing.xs },
    md: { horizontal: spacing.lg, vertical: spacing.sm },
    lg: { horizontal: spacing['2xl'], vertical: spacing.md }
  },
  
  // Padding de cards (basado en hidro-card)
  cardPadding: {
    sm: spacing.lg,     // p-4
    md: spacing['2xl'], // p-6 (default)
    lg: spacing['3xl']  // p-8
  },
  
  // Espaciado de formularios
  formSpacing: {
    fieldGap: spacing.lg,      // Espacio entre campos
    labelGap: spacing.sm,      // Espacio entre label e input
    sectionGap: spacing['2xl'] // Espacio entre secciones
  },
  
  // Espaciado de listas
  listSpacing: {
    itemGap: spacing.sm,       // Espacio entre items
    sectionGap: spacing.lg     // Espacio entre secciones
  },
  
  // Espaciado de navegación
  navigationSpacing: {
    tabPadding: spacing.xs,    // Padding interno de tabs
    tabGap: spacing.xs,        // Espacio entre tabs
    headerPadding: spacing.lg  // Padding de headers
  }
};

// Border radius (basado en Tailwind y componentes web)
export const borderRadius = {
  none: 0,
  sm: 4,   // rounded-sm
  md: 6,   // rounded-md (default)
  lg: 8,   // rounded-lg (hidro-card)
  xl: 12,  // rounded-xl
  '2xl': 16,
  '3xl': 24,
  full: 9999 // rounded-full
};

// Elevaciones/sombras (equivalentes a shadow de Tailwind)
export const elevation = {
  none: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0,
    shadowRadius: 0,
    elevation: 0
  },
  sm: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 1
  },
  md: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 6,
    elevation: 3
  },
  lg: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 10 },
    shadowOpacity: 0.15,
    shadowRadius: 15,
    elevation: 6
  },
  xl: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 20 },
    shadowOpacity: 0.25,
    shadowRadius: 25,
    elevation: 10
  }
};

// Z-index para layering
export const zIndex = {
  base: 0,
  dropdown: 1000,
  sticky: 1020,
  fixed: 1030,
  modal: 1040,
  popover: 1050,
  tooltip: 1060,
  toast: 1070
};

export type SpacingToken = keyof typeof spacing;
export type BorderRadiusToken = keyof typeof borderRadius;
export type ElevationToken = keyof typeof elevation;
export type ZIndexToken = keyof typeof zIndex;