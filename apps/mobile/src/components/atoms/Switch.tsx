import React, { useRef, useEffect } from 'react';
import { TouchableOpacity, View, ViewStyle, Animated } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Text } from './Text';

export interface SwitchProps {
  value?: boolean;
  disabled?: boolean;
  size?: 'sm' | 'md' | 'lg';
  activeColor?: string;
  inactiveColor?: string;
  thumbColor?: string;
  label?: string;
  labelPosition?: 'left' | 'right';
  style?: ViewStyle;
  onValueChange?: (value: boolean) => void;
  testID?: string;
}

const sizeStyles = {
  sm: { 
    width: 40, 
    height: 20, 
    thumbSize: 16,
    padding: 2 
  },
  md: { 
    width: 50, 
    height: 26, 
    thumbSize: 22,
    padding: 2 
  },
  lg: { 
    width: 60, 
    height: 32, 
    thumbSize: 28,
    padding: 2 
  },
} as const;

export function Switch({
  value = false,
  disabled = false,
  size = 'md',
  activeColor = semanticColors.primary,
  inactiveColor = semanticColors.backgroundSecondary,
  thumbColor = semanticColors.backgroundPrimary,
  label,
  labelPosition = 'right',
  style,
  onValueChange,
  testID,
}: SwitchProps): React.ReactElement {
  const sizeStyle = sizeStyles[size];
  const animatedValue = useRef(new Animated.Value(value ? 1 : 0)).current;
  
  useEffect(() => {
    Animated.timing(animatedValue, {
      toValue: value ? 1 : 0,
      duration: 200,
      useNativeDriver: false,
    }).start();
  }, [value, animatedValue]);
  
  const getSwitchStyle = (): ViewStyle => ({
    width: sizeStyle.width,
    height: sizeStyle.height,
    borderRadius: sizeStyle.height / 2,
    padding: sizeStyle.padding,
    justifyContent: 'center',
    opacity: disabled ? 0.6 : 1,
  });
  
  const getContainerStyle = (): ViewStyle => ({
    flexDirection: labelPosition === 'left' ? 'row-reverse' : 'row',
    alignItems: 'center',
  });
  
  const getThumbStyle = () => ({
    width: sizeStyle.thumbSize,
    height: sizeStyle.thumbSize,
    borderRadius: sizeStyle.thumbSize / 2,
    backgroundColor: disabled ? semanticColors.backgroundMuted : thumbColor,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
    transform: [
      {
        translateX: animatedValue.interpolate({
          inputRange: [0, 1],
          outputRange: [0, sizeStyle.width - sizeStyle.thumbSize - (sizeStyle.padding * 2)],
        }),
      },
    ],
  });

  const handlePress = () => {
    if (!disabled && onValueChange) {
      onValueChange(!value);
    }
  };

  return (
    <TouchableOpacity
      style={[getContainerStyle(), style]}
      onPress={handlePress}
      disabled={disabled}
      testID={testID}
      accessibilityRole="switch"
      accessibilityState={{ 
        checked: value,
        disabled 
      }}
      accessibilityLabel={label}
    >
      <Animated.View
        style={[
          getSwitchStyle(),
          {
            backgroundColor: animatedValue.interpolate({
              inputRange: [0, 1],
              outputRange: [
                disabled ? semanticColors.backgroundMuted : inactiveColor,
              disabled ? semanticColors.backgroundMuted : activeColor,
              ],
            }),
          },
        ]}
      >
        <Animated.View style={getThumbStyle()} />
      </Animated.View>
      
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