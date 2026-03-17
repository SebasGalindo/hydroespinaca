import React, { useMemo } from 'react';
import { TouchableOpacity, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius, IconName } from '@hydroespinaca/shared';
import { Icon } from './Icon';

export interface IconButtonProps {
  icon: IconName;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'default' | 'primary' | 'secondary' | 'ghost' | 'outline';
  color?: string;
  backgroundColor?: string;
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
  onPress?: () => void;
  testID?: string;
  accessibilityLabel?: string;
}

const sizeStyles = {
  sm: { 
    buttonSize: 32, 
    iconSize: 16,
    padding: spacing.xs
  },
  md: { 
    buttonSize: 40, 
    iconSize: 20,
    padding: spacing.sm
  },
  lg: { 
    buttonSize: 48, 
    iconSize: 24,
    padding: spacing.md
  },
} as const;

export const IconButton = React.memo(function IconButton({
  icon,
  size = 'md',
  variant = 'default',
  color,
  backgroundColor,
  disabled = false,
  loading = false,
  style,
  onPress,
  testID,
  accessibilityLabel,
}: IconButtonProps): React.ReactElement {
  const sizeStyle = sizeStyles[size];

  const buttonStyle = useMemo((): ViewStyle => {
    const baseStyle: ViewStyle = {
      width: sizeStyle.buttonSize,
      height: sizeStyle.buttonSize,
      borderRadius: borderRadius.md,
      alignItems: 'center',
      justifyContent: 'center',
      opacity: disabled ? 0.6 : 1,
    };

    switch (variant) {
      case 'primary':
        return { ...baseStyle, backgroundColor: backgroundColor || semanticColors.primary };
      case 'secondary':
        return { ...baseStyle, backgroundColor: backgroundColor || semanticColors.backgroundSecondary };
      case 'outline':
        return { ...baseStyle, backgroundColor: 'transparent', borderWidth: 1, borderColor: semanticColors.border };
      case 'ghost':
        return { ...baseStyle, backgroundColor: 'transparent' };
      default:
        return { ...baseStyle, backgroundColor: backgroundColor || semanticColors.backgroundMuted };
    }
  }, [variant, sizeStyle, disabled, backgroundColor]);

  const iconColor = useMemo((): string => {
    if (color) return color;
    switch (variant) {
      case 'primary':
        return semanticColors.backgroundPrimary;
      case 'ghost':
        return semanticColors.textSecondary;
      default:
        return semanticColors.textPrimary;
    }
  }, [color, variant]);

  return (
    <TouchableOpacity
      style={[buttonStyle, style]}
      onPress={onPress}
      disabled={disabled || loading}
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel || `${icon} button`}
      accessibilityState={{ disabled: disabled || loading, busy: loading }}
    >
      <Icon
        name={loading ? 'refresh' : icon}
        size={sizeStyle.iconSize}
        color={iconColor}
      />
    </TouchableOpacity>
  );
});