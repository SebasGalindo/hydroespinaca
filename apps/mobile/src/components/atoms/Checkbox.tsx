import React, { useMemo, useCallback } from 'react';
import { TouchableOpacity, View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, semanticColors, borderRadius } from '@hydroespinaca/shared';
import { Text } from './Text';
import { Icon } from './Icon';

const ICON_SIZE = 14;

export interface CheckboxProps {
  checked: boolean;
  onToggle?: (checked: boolean) => void;
  label?: string;
  disabled?: boolean;
  error?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

export const Checkbox = React.memo(function Checkbox({
  checked,
  onToggle,
  label,
  disabled = false,
  error = false,
  style,
  testID,
  accessibilityLabel,
}: CheckboxProps): React.ReactElement {
  const borderColor = useMemo(() => {
    if (error) return semanticColors.errorBorder;
    if (checked) return colors.hidro[600];
    return colors.gray[300];
  }, [error, checked]);

  const backgroundColor = useMemo(() => {
    if (checked) return colors.hidro[600];
    return colors.white;
  }, [checked]);

  const handlePress = useCallback(() => {
    if (!disabled) onToggle?.(!checked);
  }, [disabled, onToggle, checked]);

  return (
    <TouchableOpacity
      style={[styles.container, style]}
      onPress={handlePress}
      disabled={disabled}
      activeOpacity={0.7}
      testID={testID}
      accessibilityRole="checkbox"
      accessibilityLabel={accessibilityLabel || label}
      accessibilityState={{ checked, disabled }}
    >
      <View
        style={[
          styles.box,
          {
            borderColor,
            backgroundColor,
            opacity: disabled ? 0.5 : 1,
          },
        ]}
      >
        {checked && (
          <Icon name="check" size={ICON_SIZE} color={colors.white} />
        )}
      </View>
      {label && (
        <Text
          variant="body"
          color={disabled ? semanticColors.textTertiary : semanticColors.textPrimary}
          style={styles.label}
        >
          {label}
        </Text>
      )}
    </TouchableOpacity>
  );
});

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  box: {
    width: 22,
    height: 22,
    borderRadius: borderRadius.sm,
    borderWidth: 2,
    justifyContent: 'center',
    alignItems: 'center',
  },
  label: {
    marginLeft: spacing.sm,
    flex: 1,
  },
});
