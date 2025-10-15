import React, { useState, forwardRef } from 'react';
import { TextInput, View, TextStyle, ViewStyle, TextInputProps } from 'react-native';
import { semanticColors, typography, spacing, borderRadius } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface TextAreaProps extends Omit<TextInputProps, 'style' | 'multiline'> {
  variant?: 'outlined' | 'filled';
  size?: 'sm' | 'md' | 'lg';
  error?: boolean;
  disabled?: boolean;
  fullWidth?: boolean;
  rows?: number;
  maxLength?: number;
  showCharacterCount?: boolean;
  style?: TextStyle;
  containerStyle?: ViewStyle;
  testID?: string;
}

const sizeStyles = {
  sm: { 
    minHeight: 80, 
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.sm,
    fontSize: typography.fontSize.sm 
  },
  md: { 
    minHeight: 100, 
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    fontSize: typography.fontSize.md 
  },
  lg: { 
    minHeight: 120, 
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.lg,
    fontSize: typography.fontSize.lg 
  },
} as const;

export const TextArea = forwardRef<TextInput, TextAreaProps>(({
  variant = 'outlined',
  size = 'md',
  error = false,
  disabled = false,
  fullWidth = true,
  rows = 4,
  maxLength,
  showCharacterCount = false,
  style,
  containerStyle,
  testID,
  value,
  ...textInputProps
}, ref): React.ReactElement => {
  const [isFocused, setIsFocused] = useState(false);
  
  const sizeStyle = sizeStyles[size];
  const characterCount = value?.toString().length || 0;
  
  const getContainerStyle = (): ViewStyle => {
    const baseStyle: ViewStyle = {
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
    
    // filled
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
  };
  
  const getTextStyle = (): TextStyle => ({
    minHeight: rows ? rows * 20 : sizeStyle.minHeight,
    paddingHorizontal: sizeStyle.paddingHorizontal,
    paddingVertical: sizeStyle.paddingVertical,
    fontSize: sizeStyle.fontSize,
    fontFamily: typography.fontFamily.primary,
    color: disabled ? semanticColors.textMuted : semanticColors.textPrimary,
    textAlignVertical: 'top',
  });

  return (
    <View style={containerStyle}>
      <View style={getContainerStyle()} testID={testID}>
        <TextInput
          ref={ref}
          style={[getTextStyle(), style]}
          placeholderTextColor={semanticColors.textSecondary}
          editable={!disabled}
          multiline
          numberOfLines={rows}
          maxLength={maxLength}
          value={value}
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
      </View>
      
      {(showCharacterCount || maxLength) && (
        <View style={{ 
          flexDirection: 'row', 
          justifyContent: 'flex-end', 
          marginTop: spacing.xs 
        }}>
          <Text
            style={{
              fontSize: typography.fontSize.xs,
              color: maxLength && characterCount > maxLength 
                ? semanticColors.errorText 
                : semanticColors.textSecondary,
            }}
          >
            {`${characterCount}${maxLength ? `/${maxLength}` : ''}`}
          </Text>
        </View>
      )}
    </View>
  );
});