import React from 'react';
import { TouchableOpacity, ViewStyle, ActivityIndicator } from 'react-native';
import { semanticColors, spacing, borderRadius, typography } from '@hidroespinaca/shared';
import { Text } from './Text';

export interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  fullWidth?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  style?: ViewStyle | ViewStyle[];
  onPress?: () => void;
  testID?: string;
  accessibilityLabel?: string;
}

const sizeStyles = {
  sm: {
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.sm,
    minHeight: 32,
    fontSize: typography.fontSize.sm,
  },
  md: {
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    minHeight: 40,
    fontSize: typography.fontSize.md,
  },
  lg: {
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
    minHeight: 48,
    fontSize: typography.fontSize.lg,
  },
} as const;

export function Button({
  children,
  variant = 'primary',
  size = 'md',
  disabled = false,
  loading = false,
  fullWidth = false,
  leftIcon,
  rightIcon,
  style,
  onPress,
  testID,
  accessibilityLabel,
}: ButtonProps): React.ReactElement {
  const sizeStyle = sizeStyles[size];
  
  const getButtonStyle = (): ViewStyle => {
    const baseStyle: ViewStyle = {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      borderRadius: borderRadius.md,
      paddingVertical: sizeStyle.paddingVertical,
      paddingHorizontal: sizeStyle.paddingHorizontal,
      minHeight: sizeStyle.minHeight,
      opacity: disabled || loading ? 0.6 : 1,
      width: fullWidth ? '100%' : undefined,
    };
    
    switch (variant) {
      case 'primary':
        return {
          ...baseStyle,
          backgroundColor: semanticColors.primary,
        };
      case 'secondary':
        return {
          ...baseStyle,
          backgroundColor: semanticColors.backgroundSecondary,
        };
      case 'outline':
        return {
          ...baseStyle,
          backgroundColor: 'transparent',
          borderWidth: 1,
          borderColor: semanticColors.border,
        };
      case 'ghost':
        return {
          ...baseStyle,
          backgroundColor: 'transparent',
        };
      case 'danger':
        return {
          ...baseStyle,
          backgroundColor: semanticColors.errorBg,
        };
      default:
        return baseStyle;
    }
  };
  
  const getTextColor = (): string => {
    switch (variant) {
      case 'primary':
        return semanticColors.textInverse;
      case 'secondary':
        return semanticColors.textPrimary;
      case 'outline':
        return semanticColors.textPrimary;
      case 'ghost':
        return semanticColors.textSecondary;
      case 'danger':
        return semanticColors.textPrimary;
      default:
        return semanticColors.textPrimary;
    }
  };
  
  const getLoadingColor = (): string => {
    switch (variant) {
      case 'primary':
        return semanticColors.textInverse;
      case 'danger':
        return semanticColors.textInverse;
      default:
        return semanticColors.textPrimary;
    }
  };
  
  const renderContent = () => {
    if (loading) {
      return (
        <ActivityIndicator
          size="small"
          color={getLoadingColor()}
          testID={`${testID}-loading`}
        />
      );
    }
    
    return (
      <>
        {leftIcon && (
          <Text style={{ marginRight: spacing.xs }}>
            {leftIcon}
          </Text>
        )}
        
        <Text
          style={{
            fontSize: sizeStyle.fontSize,
            fontWeight: typography.fontWeight.medium as any,
            color: getTextColor(),
            textAlign: 'center',
          }}
        >
          {children}
        </Text>
        
        {rightIcon && (
          <Text style={{ marginLeft: spacing.xs }}>
            {rightIcon}
          </Text>
        )}
      </>
    );
  };
  
  return (
    <TouchableOpacity
      style={[getButtonStyle(), style]}
      onPress={onPress}
      disabled={disabled || loading}
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel || (typeof children === 'string' ? children : 'Button')}
      accessibilityState={{ 
        disabled: disabled || loading,
        busy: loading
      }}
    >
      {renderContent()}
    </TouchableOpacity>
  );
}