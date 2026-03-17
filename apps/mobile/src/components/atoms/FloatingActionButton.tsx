import React, { useCallback } from 'react';
import { TouchableOpacity, StyleSheet, ViewStyle, Platform } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { colors, shadows, spacing } from '@hydroespinaca/shared';
import { hapticLight } from '../../utils/haptics';

export interface FloatingActionButtonProps {
  icon?: keyof typeof Ionicons.glyphMap;
  onPress?: () => void;
  color?: string;
  backgroundColor?: string;
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

const sizeMap = {
  sm: { button: 44, icon: 22 },
  md: { button: 56, icon: 28 },
  lg: { button: 64, icon: 32 },
} as const;

export const FloatingActionButton = React.memo(function FloatingActionButton({
  icon = 'add',
  onPress,
  color = colors.white,
  backgroundColor = colors.hidro[600],
  size = 'md',
  disabled = false,
  style,
  testID,
  accessibilityLabel = 'Agregar',
}: FloatingActionButtonProps): React.ReactElement {
  const sizeStyle = sizeMap[size];

  const handlePress = useCallback(() => {
    if (disabled) return;
    hapticLight();
    onPress?.();
  }, [disabled, onPress]);

  return (
    <TouchableOpacity
      style={[
        styles.container,
        {
          width: sizeStyle.button,
          height: sizeStyle.button,
          borderRadius: sizeStyle.button / 2,
          backgroundColor,
          opacity: disabled ? 0.5 : 1,
        },
        style,
      ]}
      onPress={handlePress}
      disabled={disabled}
      activeOpacity={0.8}
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      accessibilityState={{ disabled }}
    >
      <Ionicons name={icon} size={sizeStyle.icon} color={color} />
    </TouchableOpacity>
  );
});

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    bottom: spacing.lg,
    right: spacing.lg,
    justifyContent: 'center',
    alignItems: 'center',
    ...shadows.lg,
    elevation: 8,
  },
});
