import React from 'react';
import {
  Switch as RNSwitch,
  View,
  StyleSheet,
  ViewStyle,
  Platform,
} from 'react-native';
import { colors, spacing, semanticColors } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface SwitchProps {
  value: boolean;
  onValueChange?: (value: boolean) => void;
  label?: string;
  disabled?: boolean;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

export function Switch({
  value,
  onValueChange,
  label,
  disabled = false,
  style,
  testID,
  accessibilityLabel,
}: SwitchProps): React.ReactElement {
  return (
    <View style={[styles.container, style]}>
      {label && (
        <Text
          variant="body"
          color={disabled ? semanticColors.textTertiary : semanticColors.textPrimary}
          style={styles.label}
        >
          {label}
        </Text>
      )}
      <RNSwitch
        value={value}
        onValueChange={onValueChange}
        disabled={disabled}
        trackColor={{
          false: colors.gray[300],
          true: colors.hidro[400],
        }}
        thumbColor={
          Platform.OS === 'android'
            ? value ? colors.hidro[600] : colors.gray[100]
            : colors.white
        }
        ios_backgroundColor={colors.gray[300]}
        testID={testID}
        accessibilityRole="switch"
        accessibilityLabel={accessibilityLabel || label}
        accessibilityState={{ checked: value, disabled }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  label: {
    flex: 1,
    marginRight: spacing.md,
  },
});
