import React from 'react';
import { TouchableOpacity, View, ViewStyle } from 'react-native';
import { semanticColors, spacing, typography } from '@hidroespinaca/shared';
import { Text } from './Text';

export interface RadioProps {
  selected?: boolean;
  disabled?: boolean;
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  label?: string;
  labelPosition?: 'left' | 'right';
  value?: string | number;
  style?: ViewStyle;
  onPress?: (value?: string | number) => void;
  testID?: string;
}

const sizeStyles = {
  sm: { size: 20, innerSize: 8 },
  md: { size: 24, innerSize: 10 },
  lg: { size: 32, innerSize: 14 },
} as const;

export function Radio({
  selected = false,
  disabled = false,
  size = 'md',
  color = semanticColors.primary,
  label,
  labelPosition = 'right',
  value,
  style,
  onPress,
  testID,
}: RadioProps): React.ReactElement {
  const sizeStyle = sizeStyles[size];
  
  const getRadioStyle = (): ViewStyle => ({
    width: sizeStyle.size,
    height: sizeStyle.size,
    borderRadius: sizeStyle.size / 2,
    borderWidth: 2,
    borderColor: disabled 
      ? semanticColors.borderDisabled 
      : selected 
        ? color 
        : semanticColors.textSecondary,
    backgroundColor: disabled 
        ? semanticColors.backgroundMuted 
        : semanticColors.background,
    alignItems: 'center',
    justifyContent: 'center',
  });
  
  const getContainerStyle = (): ViewStyle => ({
    flexDirection: labelPosition === 'left' ? 'row-reverse' : 'row',
    alignItems: 'center',
    opacity: disabled ? 0.6 : 1,
  });
  
  const getInnerCircleStyle = (): ViewStyle => ({
    width: sizeStyle.innerSize,
    height: sizeStyle.innerSize,
    borderRadius: sizeStyle.innerSize / 2,
    backgroundColor: disabled ? semanticColors.textMuted : color,
  });

  const handlePress = () => {
    if (!disabled && onPress) {
      onPress(value);
    }
  };

  return (
    <TouchableOpacity
      style={[getContainerStyle(), style]}
      onPress={handlePress}
      disabled={disabled}
      testID={testID}
      accessibilityRole="radio"
      accessibilityState={{ 
        selected,
        disabled 
      }}
      accessibilityLabel={label}
    >
      <View style={getRadioStyle()}>
        {selected && <View style={getInnerCircleStyle()} />}
      </View>
      {label ? (
        <Text
          // Forzar visibilidad del texto con tamaño/lineHeight calculado correctamente
          style={{
            marginLeft: labelPosition === 'right' ? spacing.sm : 0,
            marginRight: labelPosition === 'left' ? spacing.sm : 0,
            color: disabled ? semanticColors.textMuted : semanticColors.textPrimary,
            fontSize: typography.fontSize.base,
          }}
        >
          {label}
        </Text>
      ) : null}
    </TouchableOpacity>
  );
}