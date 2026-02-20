import React, { useState } from 'react';
import { View, TextInput, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius, typography, semanticColors } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';
import { TouchableOpacity } from 'react-native';

export interface SearchBarProps {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  onClear?: () => void;
  style?: ViewStyle;
  testID?: string;
}

export function SearchBar({
  value,
  onChangeText,
  placeholder = 'Buscar...',
  onClear,
  style,
  testID,
}: SearchBarProps): React.ReactElement {
  const [isFocused, setIsFocused] = useState(false);

  const handleClear = () => {
    onChangeText('');
    onClear?.();
  };

  return (
    <View
      style={[
        styles.container,
        { borderColor: isFocused ? colors.hidro[500] : colors.gray[300] },
        style,
      ]}
      testID={testID}
    >
      <Icon name="search" size={20} color={semanticColors.textTertiary} />
      <TextInput
        style={styles.input}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={semanticColors.textTertiary}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        returnKeyType="search"
        accessibilityRole="search"
        accessibilityLabel={placeholder}
      />
      {value.length > 0 && (
        <TouchableOpacity
          onPress={handleClear}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          accessibilityLabel="Limpiar búsqueda"
        >
          <Icon name="close" size={18} color={semanticColors.textTertiary} />
        </TouchableOpacity>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: semanticColors.surface,
    borderWidth: 1,
    borderRadius: borderRadius.lg,
    paddingHorizontal: spacing.md,
    height: 44,
    gap: spacing.sm,
  },
  input: {
    flex: 1,
    fontSize: typography.fontSize.md,
    color: semanticColors.textPrimary,
    padding: 0,
  },
});
