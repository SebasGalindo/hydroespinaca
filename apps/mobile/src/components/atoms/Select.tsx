import React, { useState } from 'react';
import {
  View,
  TouchableOpacity,
  Modal,
  FlatList,
  StyleSheet,
  ViewStyle,
} from 'react-native';
import { semanticColors, spacing, borderRadius, typography, colors } from '@hydroespinaca/shared';
import { Text } from './Text';
import { Icon } from './Icon';

export interface SelectOption {
  label: string;
  value: string;
}

export interface SelectProps {
  options: SelectOption[];
  value?: string;
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  fullWidth?: boolean;
  onValueChange?: (value: string) => void;
  style?: ViewStyle;
  testID?: string;
  accessibilityLabel?: string;
}

export function Select({
  options,
  value,
  placeholder = 'Seleccionar...',
  disabled = false,
  error = false,
  fullWidth = true,
  onValueChange,
  style,
  testID,
  accessibilityLabel,
}: SelectProps): React.ReactElement {
  const [isOpen, setIsOpen] = useState(false);

  const selectedOption = options.find((opt) => opt.value === value);

  const handleSelect = (optionValue: string) => {
    onValueChange?.(optionValue);
    setIsOpen(false);
  };

  const getBorderColor = () => {
    if (error) return semanticColors.errorBorder;
    return colors.gray[300];
  };

  return (
    <>
      <TouchableOpacity
        style={[
          styles.trigger,
          {
            borderColor: getBorderColor(),
            opacity: disabled ? 0.5 : 1,
          },
          fullWidth && styles.fullWidth,
          style,
        ]}
        onPress={() => !disabled && setIsOpen(true)}
        disabled={disabled}
        testID={testID}
        accessibilityRole="combobox"
        accessibilityLabel={accessibilityLabel || placeholder}
        accessibilityState={{ expanded: isOpen, disabled }}
      >
        <Text
          variant="body"
          color={selectedOption ? semanticColors.textPrimary : semanticColors.textTertiary}
          style={styles.triggerText}
          numberOfLines={1}
        >
          {selectedOption ? selectedOption.label : placeholder}
        </Text>
        <Icon
          name="chevron-down"
          size={18}
          color={semanticColors.textTertiary}
        />
      </TouchableOpacity>

      <Modal
        visible={isOpen}
        transparent
        animationType="fade"
        onRequestClose={() => setIsOpen(false)}
      >
        <TouchableOpacity
          style={styles.overlay}
          activeOpacity={1}
          onPress={() => setIsOpen(false)}
        >
          <View style={styles.dropdown}>
            <View style={styles.dropdownHeader}>
              <Text variant="label" color={semanticColors.textPrimary}>
                {accessibilityLabel || placeholder}
              </Text>
              <TouchableOpacity onPress={() => setIsOpen(false)}>
                <Icon name="close" size={22} color={semanticColors.textSecondary} />
              </TouchableOpacity>
            </View>
            <FlatList
              data={options}
              keyExtractor={(item) => item.value}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={[
                    styles.option,
                    item.value === value && styles.optionSelected,
                  ]}
                  onPress={() => handleSelect(item.value)}
                  accessibilityRole="radio"
                  accessibilityState={{ selected: item.value === value }}
                >
                  <Text
                    variant="body"
                    color={
                      item.value === value
                        ? semanticColors.primary
                        : semanticColors.textPrimary
                    }
                  >
                    {item.label}
                  </Text>
                  {item.value === value && (
                    <Icon name="check" size={20} color={semanticColors.primary} />
                  )}
                </TouchableOpacity>
              )}
              style={styles.optionsList}
            />
          </View>
        </TouchableOpacity>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  trigger: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    minHeight: 44,
    paddingHorizontal: spacing.md,
    borderWidth: 1,
    borderRadius: borderRadius.md,
    backgroundColor: semanticColors.surface,
  },
  fullWidth: {
    width: '100%',
  },
  triggerText: {
    flex: 1,
    marginRight: spacing.sm,
  },
  overlay: {
    flex: 1,
    backgroundColor: semanticColors.overlayMedium,
    justifyContent: 'center',
    padding: spacing.xl,
  },
  dropdown: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    maxHeight: 400,
    overflow: 'hidden',
  },
  dropdownHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
  },
  optionsList: {
    maxHeight: 340,
  },
  option: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.gray[100],
  },
  optionSelected: {
    backgroundColor: colors.hidro[50],
  },
});
