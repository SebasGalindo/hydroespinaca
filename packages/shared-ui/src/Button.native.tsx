import React from 'react';
import { TouchableOpacity, Text, ActivityIndicator, TextStyle, ViewStyle } from 'react-native';

export interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  onPress?: () => void;
  style?: ViewStyle;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  disabled = false,
  loading = false,
  fullWidth = false,
  onPress,
  style,
}) => {
  const getButtonStyles = (
    variant: string,
    size: string,
    disabled: boolean,
    fullWidth: boolean
  ): ViewStyle => {
    const baseStyles: ViewStyle = {
      borderRadius: 6,
      alignItems: 'center',
      justifyContent: 'center',
      flexDirection: 'row',
      width: fullWidth ? '100%' : 'auto',
    };

    const variantStyles: Record<string, ViewStyle> = {
      primary: {
        backgroundColor: disabled ? '#9ca3af' : '#4CAF50',
      },
      secondary: {
        backgroundColor: disabled ? '#f3f4f6' : '#6b7280',
      },
      outline: {
        backgroundColor: 'transparent',
        borderWidth: 1,
        borderColor: disabled ? '#d1d5db' : '#4CAF50',
      },
      ghost: {
        backgroundColor: 'transparent',
      },
    };

    const sizeStyles: Record<string, ViewStyle> = {
      sm: {
        paddingVertical: 8,
        paddingHorizontal: 12,
      },
      md: {
        paddingVertical: 12,
        paddingHorizontal: 16,
      },
      lg: {
        paddingVertical: 16,
        paddingHorizontal: 24,
      },
    };

    return {
      ...baseStyles,
      ...variantStyles[variant],
      ...sizeStyles[size],
    };
  };

  const getTextStyles = (
    variant: string,
    size: string,
    disabled: boolean
  ): TextStyle => {
    const baseStyles: TextStyle = {
      fontWeight: '600',
      textAlign: 'center',
    };

    const variantStyles: Record<string, TextStyle> = {
      primary: {
        color: '#ffffff',
      },
      secondary: {
        color: '#ffffff',
      },
      outline: {
        color: disabled ? '#9ca3af' : '#4CAF50',
      },
      ghost: {
        color: disabled ? '#9ca3af' : '#374151',
      },
    };

    const sizeStyles: Record<string, TextStyle> = {
      sm: {
        fontSize: 14,
      },
      md: {
        fontSize: 16,
      },
      lg: {
        fontSize: 18,
      },
    };

    return {
      ...baseStyles,
      ...variantStyles[variant],
      ...sizeStyles[size],
    };
  };

  const buttonStyles = getButtonStyles(variant, size, disabled, fullWidth);
  const textStyles = getTextStyles(variant, size, disabled);

  return (
    <TouchableOpacity
      style={[buttonStyles, style]}
      onPress={onPress}
      disabled={disabled || loading}
      activeOpacity={disabled ? 1 : 0.7}
    >
      {loading && (
        <ActivityIndicator
          size="small"
          color={variant === 'primary' || variant === 'secondary' ? '#ffffff' : '#4CAF50'}
          style={{ marginRight: 8 }}
        />
      )}
      <Text style={textStyles}>{children}</Text>
    </TouchableOpacity>
  );
};