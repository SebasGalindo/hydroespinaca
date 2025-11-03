import React, { useState, forwardRef } from 'react';
import { TextInput, View, TextStyle, ViewStyle, TextInputProps } from 'react-native';
import { semanticColors, typography, spacing, borderRadius } from '@hydroespinaca/shared';

export interface InputProps extends Omit<TextInputProps, 'style'> {
  variant?: 'outlined' | 'filled' | 'underlined';
  size?: 'sm' | 'md' | 'lg';
  error?: boolean;
  disabled?: boolean;
  fullWidth?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  style?: TextStyle;
  containerStyle?: ViewStyle;
  testID?: string;
}

const sizeStyles = {
  sm: { 
    minHeight: 36, 
    paddingHorizontal: spacing.sm, 
    fontSize: typography.fontSize.sm 
  },
  md: { 
    minHeight: 44, 
    paddingHorizontal: spacing.md, 
    fontSize: typography.fontSize.md 
  },
  lg: { 
    minHeight: 52, 
    paddingHorizontal: spacing.lg, 
    fontSize: typography.fontSize.lg 
  },
} as const;

export const Input = forwardRef<TextInput, InputProps>(({
  variant = 'outlined',
  size = 'md',
  error = false,
  disabled = false,
  fullWidth = true,
  leftIcon,
  rightIcon,
  style,
  containerStyle,
  testID,
  ...textInputProps
}, ref): React.ReactElement => {
  const [isFocused, setIsFocused] = useState(false);
  
  const sizeStyle = sizeStyles[size];
  
  const getContainerStyle = (): ViewStyle => {
    const baseStyle: ViewStyle = {
      flexDirection: 'row',
      alignItems: 'center',
      borderRadius: borderRadius.md,
      ...(fullWidth && { width: '100%' }),
    };
    
    if (variant === 'outlined') {
      return {
        ...baseStyle,
        borderWidth: 1,
        borderColor: error 
          ? semanticColors.errorText 
          : isFocused 
            ? semanticColors.borderFocus 
            : semanticColors.border,
        backgroundColor: disabled ? semanticColors.backgroundMuted : semanticColors.background,
      };
    }
    
    if (variant === 'filled') {
      return {
        ...baseStyle,
        backgroundColor: disabled 
        ? semanticColors.backgroundMuted 
        : semanticColors.backgroundSecondary,
      borderBottomWidth: 2,
      borderBottomColor: error 
        ? semanticColors.errorText 
        : isFocused 
          ? semanticColors.borderFocus 
          : 'transparent',
      };
    }
    
    // underlined
    return {
      ...baseStyle,
      backgroundColor: 'transparent',
      borderBottomWidth: 1,
      borderBottomColor: error 
        ? semanticColors.errorText 
        : isFocused 
          ? semanticColors.primary 
          : semanticColors.border,
      borderRadius: 0,
    };
  };
  
  const getTextStyle = (): TextStyle => ({
    flex: 1,
    minHeight: sizeStyle.minHeight,
    paddingHorizontal: leftIcon || rightIcon ? spacing.xs : sizeStyle.paddingHorizontal,
    paddingVertical: spacing.sm,
    fontSize: sizeStyle.fontSize,
    fontFamily: typography.fontFamily.primary,
    color: disabled ? semanticColors.textMuted : semanticColors.textPrimary,
  });

  return (
    <View style={[getContainerStyle(), containerStyle]} testID={testID}>
      {leftIcon && (
        <View style={{ paddingLeft: sizeStyle.paddingHorizontal }} pointerEvents="none">
          {leftIcon}
        </View>
      )}

      <TextInput
        ref={ref}
        style={[getTextStyle(), style]}
        placeholderTextColor={semanticColors.textPlaceholder}
        editable={!disabled}
        onFocus={(e) => {
          setIsFocused(true);
          textInputProps.onFocus?.(e);
        }}
        onBlur={(e) => {
          setIsFocused(false);
          textInputProps.onBlur?.(e);
        }}
        {...textInputProps}
      />

      {rightIcon && (
        <View style={{ paddingRight: sizeStyle.paddingHorizontal }}>
          {rightIcon}
        </View>
      )}
    </View>
  );
});