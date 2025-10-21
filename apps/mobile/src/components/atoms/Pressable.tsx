import React from 'react';
import { 
  Pressable as RNPressable, 
  PressableProps as RNPressableProps,
  ViewStyle,
  PressableStateCallbackType
} from 'react-native';
import { semanticColors } from '@hydroespinaca/shared';

// Tipo extendido para incluir estados adicionales
interface ExtendedPressableState extends PressableStateCallbackType {
  hovered?: boolean;
  focused?: boolean;
}

export interface PressableProps extends Omit<RNPressableProps, 'style'> {
  children: React.ReactNode | ((state: PressableStateCallbackType) => React.ReactNode);
  style?: ViewStyle | ((state: PressableStateCallbackType) => ViewStyle);
  pressedStyle?: ViewStyle;
  hoveredStyle?: ViewStyle;
  focusedStyle?: ViewStyle;
  disabledStyle?: ViewStyle;
  pressedOpacity?: number;
  rippleColor?: string;
  borderless?: boolean;
  testID?: string;
}

export function Pressable({
  children,
  style,
  pressedStyle,
  hoveredStyle,
  focusedStyle,
  disabledStyle,
  pressedOpacity = 0.7,
  rippleColor = semanticColors.primary,
  borderless = false,
  disabled = false,
  testID,
  ...props
}: PressableProps): React.ReactElement {
  
  const getStyle = (state: PressableStateCallbackType): ViewStyle => {
    let baseStyle: ViewStyle = {};
    const extendedState = state as ExtendedPressableState;
    
    // Aplicar estilo base
    if (typeof style === 'function') {
      baseStyle = style(state);
    } else if (style) {
      baseStyle = style;
    }
    
    // Aplicar estilos de estado
    if (disabled && disabledStyle) {
      return { ...baseStyle, ...disabledStyle };
    }
    
    if (state.pressed && pressedStyle) {
      return { 
        ...baseStyle, 
        ...pressedStyle,
        opacity: pressedOpacity
      };
    }
    
    if (extendedState.hovered && hoveredStyle) {
      return { ...baseStyle, ...hoveredStyle };
    }
    
    if (extendedState.focused && focusedStyle) {
      return { ...baseStyle, ...focusedStyle };
    }
    
    // Aplicar opacidad por defecto cuando está presionado
    if (state.pressed) {
      return { 
        ...baseStyle, 
        opacity: pressedOpacity 
      };
    }
    
    return baseStyle;
  };
  
  return (
    <RNPressable
      style={getStyle}
      disabled={disabled}
      testID={testID}
      accessibilityRole="button"
      accessibilityState={{ disabled: Boolean(disabled) }}
      android_ripple={{
        color: rippleColor,
        borderless,
      }}
      {...props}
    >
      {children}
    </RNPressable>
  );
}