import React from 'react';
import { TouchableOpacity, View, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius } from '@hidroespinaca/shared';
import { Text } from './Text';

export interface CheckboxProps {
  checked?: boolean;
  indeterminate?: boolean;
  disabled?: boolean;
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  label?: string;
  labelPosition?: 'left' | 'right';
  style?: ViewStyle;
  onPress?: (checked: boolean) => void;
  testID?: string;
}

const sizeStyles = {
  sm: { size: 16, iconSize: 10 },
  md: { size: 20, iconSize: 12 },
  lg: { size: 24, iconSize: 14 },
} as const;

export function Checkbox({
  checked = false,
  indeterminate = false,
  disabled = false,
  size = 'md',
  color = semanticColors.primary,
  label,
  labelPosition = 'right',
  style,
  onPress,
  testID,
}: CheckboxProps): React.ReactElement {
  const sizeStyle = sizeStyles[size];
  
  const getCheckboxStyle = (): ViewStyle => ({
    width: sizeStyle.size,
    height: sizeStyle.size,
    borderRadius: borderRadius.sm,
    borderWidth: 2,
    borderColor: disabled 
      ? semanticColors.borderDisabled 
      : checked || indeterminate 
        ? color 
        : semanticColors.border,
    backgroundColor: disabled 
      ? semanticColors.backgroundMuted 
      : checked || indeterminate 
        ? color 
        : 'transparent',
    alignItems: 'center',
    justifyContent: 'center',
  });
  
  const getContainerStyle = (): ViewStyle => ({
    flexDirection: labelPosition === 'left' ? 'row-reverse' : 'row',
    alignItems: 'center',
    opacity: disabled ? 0.6 : 1,
  });
  
  const renderIcon = () => {
    if (indeterminate) {
      return (
        <View
          style={{
            width: sizeStyle.iconSize,
            height: 2,
            backgroundColor: disabled ? semanticColors.textMuted : semanticColors.backgroundPrimary,
          }}
        />
      );
    }
    
    if (checked) {
      return (
        <Text
          style={{
            fontSize: sizeStyle.iconSize,
            color: disabled ? semanticColors.textMuted : semanticColors.backgroundPrimary,
            fontWeight: 'bold',
          }}
        >
          ✓
        </Text>
      );
    }
    
    return null;
  };

  const handlePress = () => {
    if (!disabled && onPress) {
      onPress(!checked);
    }
  };

  return (
    <TouchableOpacity
      style={[getContainerStyle(), style]}
      onPress={handlePress}
      disabled={disabled}
      testID={testID}
      accessibilityRole="checkbox"
      accessibilityState={{ 
        checked: indeterminate ? 'mixed' : checked,
        disabled 
      }}
      accessibilityLabel={label}
    >
      <View style={getCheckboxStyle()}>
        {renderIcon()}
      </View>
      
      {label && (
        <Text
          style={{
            marginLeft: labelPosition === 'right' ? spacing.sm : 0,
            marginRight: labelPosition === 'left' ? spacing.sm : 0,
            color: disabled ? semanticColors.textMuted : semanticColors.textPrimary,
          }}
        >
          {label}
        </Text>
      )}
    </TouchableOpacity>
  );
}