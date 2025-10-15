import React, { useState } from 'react';
import { 
  View, 
  TouchableOpacity, 
  Modal, 
  FlatList, 
  ViewStyle, 
  TextStyle 
} from 'react-native';
import { semanticColors, typography, spacing, borderRadius } from '@hydroespinaca/shared';
import { Text } from './Text';

export interface SelectOption {
  label: string;
  value: string | number;
  disabled?: boolean;
}

export interface SelectProps {
  options: SelectOption[];
  value?: string | number;
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  size?: 'sm' | 'md' | 'lg';
  fullWidth?: boolean;
  style?: ViewStyle;
  onSelect: (option: SelectOption) => void;
  testID?: string;
}

const sizeStyles = {
  sm: { 
    minHeight: 36, 
    paddingHorizontal: spacing.sm, 
    fontSize: typography.fontSize.sm 
  },
  md: { 
    minHeight: 44, 
    paddingHorizontal: spacing.md, 
    fontSize: typography.fontSize.md 
  },
  lg: { 
    minHeight: 52, 
    paddingHorizontal: spacing.lg, 
    fontSize: typography.fontSize.lg 
  },
} as const;

export function Select({
  options,
  value,
  placeholder = 'Seleccionar...',
  disabled = false,
  error = false,
  size = 'md',
  fullWidth = true,
  style,
  onSelect,
  testID,
}: SelectProps): React.ReactElement {
  const [isOpen, setIsOpen] = useState(false);
  
  const sizeStyle = sizeStyles[size];
  const selectedOption = options.find(option => option.value === value);
  
  const getContainerStyle = (): ViewStyle => ({
    minHeight: sizeStyle.minHeight,
    paddingHorizontal: sizeStyle.paddingHorizontal,
    paddingVertical: spacing.sm,
    borderWidth: 1,
    borderColor: error 
      ? semanticColors.errorText 
      : semanticColors.border,
    borderRadius: borderRadius.md,
    backgroundColor: disabled 
      ? semanticColors.backgroundMuted 
      : semanticColors.background,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    ...(fullWidth && { width: '100%' }),
  });
  
  const getTextStyle = (): TextStyle => ({
    fontSize: sizeStyle.fontSize,
    color: disabled 
      ? semanticColors.textMuted 
      : selectedOption 
        ? semanticColors.textPrimary 
        : semanticColors.textPlaceholder,
  });

  const handleSelect = (option: SelectOption) => {
    if (!option.disabled) {
      onSelect(option);
      setIsOpen(false);
    }
  };

  return (
    <>
      <TouchableOpacity
        style={[getContainerStyle(), style]}
        onPress={() => !disabled && setIsOpen(true)}
        disabled={disabled}
        testID={testID}
        accessibilityRole="button"
        accessibilityLabel={selectedOption?.label || placeholder}
      >
        <Text style={getTextStyle()}>
          {selectedOption?.label || placeholder}
        </Text>
        
        <Text style={{ 
          fontSize: sizeStyle.fontSize, 
          color: semanticColors.textSecondary 
        }}>
          ▼
        </Text>
      </TouchableOpacity>

      <Modal
        visible={isOpen}
        transparent
        animationType="fade"
        onRequestClose={() => setIsOpen(false)}
      >
        <TouchableOpacity
          style={{
            flex: 1,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            justifyContent: 'center',
            alignItems: 'center',
          }}
          onPress={() => setIsOpen(false)}
        >
          <View
            style={{
              backgroundColor: semanticColors.surface,
              borderRadius: borderRadius.lg,
              maxHeight: 300,
              width: '80%',
              maxWidth: 400,
            }}
          >
            <FlatList
              data={options}
              keyExtractor={(item) => item.value.toString()}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={{
                    paddingVertical: spacing.md,
                    paddingHorizontal: spacing.lg,
                    borderBottomWidth: 1,
                    borderBottomColor: semanticColors.border,
                    backgroundColor: item.disabled
                      ? semanticColors.backgroundMuted
                      : (item.value === value ? semanticColors.infoBg : 'transparent'),
                  }}
                  onPress={() => handleSelect(item)}
                  disabled={item.disabled}
                >
                  <Text
                    style={{
                      fontSize: typography.fontSize.md,
                      color: item.disabled
                        ? semanticColors.textMuted
                        : (item.value === value ? semanticColors.primary : semanticColors.textPrimary),
                      fontWeight: item.value === value
                        ? (typography.fontWeight.semibold as any)
                        : (typography.fontWeight.normal as any),
                    }}
                  >
                    {item.label}
                  </Text>
                </TouchableOpacity>
              )}
            />
          </View>
        </TouchableOpacity>
      </Modal>
    </>
  );
}