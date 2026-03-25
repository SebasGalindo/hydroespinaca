import React, { useState, useCallback } from 'react';
import { View, TextInput, StyleSheet, ViewStyle, TouchableOpacity } from 'react-native';
import { colors, spacing, borderRadius, typography, semanticColors } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

export interface SearchBarProps {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  onClear?: () => void;
  style?: ViewStyle;
  testID?: string;
}

export const SearchBar = React.memo(function SearchBar({
  value,
  onChangeText,
  placeholder = 'Buscar...',
  onClear,
  style,
  testID,
}: SearchBarProps): React.ReactElement {
  const [isFocused, setIsFocused] = useState(false);

  const handleClear = useCallback(() => {
    onChangeText('');
    onClear?.();
  }, [onChangeText, onClear]);

  const handleFocus = useCallback(() => setIsFocused(true), []);
  const handleBlur = useCallback(() => setIsFocused(false), []);

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
        onFocus={handleFocus}
        onBlur={handleBlur}
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
});

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
