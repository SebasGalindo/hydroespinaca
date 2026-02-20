import React, { useState, forwardRef } from 'react';
import { TextInput, View, StyleSheet, TextStyle, ViewStyle, TextInputProps } from 'react-native';
import { semanticColors, typography, spacing, borderRadius, colors } from '@hydroespinaca/shared';

export interface TextAreaProps extends Omit<TextInputProps, 'style'> {
  minHeight?: number;
  maxHeight?: number;
  error?: boolean;
  disabled?: boolean;
  fullWidth?: boolean;
  style?: TextStyle;
  containerStyle?: ViewStyle;
  testID?: string;
}

export const TextArea = forwardRef<TextInput, TextAreaProps>(({
  minHeight = 100,
  maxHeight = 200,
  error = false,
  disabled = false,
  fullWidth = true,
  style,
  containerStyle,
  testID,
  ...textInputProps
}, ref): React.ReactElement => {
  const [isFocused, setIsFocused] = useState(false);

  const getBorderColor = () => {
    if (error) return semanticColors.errorBorder;
    if (isFocused) return colors.hidro[500];
    return colors.gray[300];
  };

  return (
    <View
      style={[
        styles.container,
        {
          borderColor: getBorderColor(),
          opacity: disabled ? 0.5 : 1,
        },
        fullWidth && styles.fullWidth,
        containerStyle,
      ]}
    >
      <TextInput
        ref={ref}
        style={[
          styles.input,
          {
            minHeight,
            maxHeight,
          },
          style,
        ]}
        multiline
        textAlignVertical="top"
        editable={!disabled}
        onFocus={(e) => {
          setIsFocused(true);
          textInputProps.onFocus?.(e);
        }}
        onBlur={(e) => {
          setIsFocused(false);
          textInputProps.onBlur?.(e);
        }}
        placeholderTextColor={semanticColors.textTertiary}
        testID={testID}
        accessibilityState={{ disabled }}
        {...textInputProps}
      />
    </View>
  );
});

TextArea.displayName = 'TextArea';

const styles = StyleSheet.create({
  container: {
    borderWidth: 1,
    borderRadius: borderRadius.md,
    backgroundColor: colors.white,
    overflow: 'hidden',
  },
  fullWidth: {
    width: '100%',
  },
  input: {
    fontSize: typography.fontSize.md,
    color: semanticColors.textPrimary,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    lineHeight: 22,
  },
});
