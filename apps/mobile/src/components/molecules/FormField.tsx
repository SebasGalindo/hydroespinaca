import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { spacing, semanticColors } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';

export interface FormFieldProps {
  label?: string;
  error?: string;
  helperText?: string;
  required?: boolean;
  children: React.ReactNode;
  style?: ViewStyle;
  testID?: string;
}

export function FormField({
  label,
  error,
  helperText,
  required = false,
  children,
  style,
  testID,
}: FormFieldProps): React.ReactElement {
  return (
    <View style={[styles.container, style]} testID={testID}>
      {label && (
        <View style={styles.labelContainer}>
          <Text variant="label" color={semanticColors.textSecondary}>
            {label}
          </Text>
          {required && (
            <Text variant="label" color={semanticColors.errorText}>
              {' *'}
            </Text>
          )}
        </View>
      )}
      {children}
      {error ? (
        <Text variant="caption" color={semanticColors.errorText} style={styles.helperText}>
          {error}
        </Text>
      ) : helperText ? (
        <Text variant="caption" color={semanticColors.textTertiary} style={styles.helperText}>
          {helperText}
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.md,
  },
  labelContainer: {
    flexDirection: 'row',
    marginBottom: spacing.xs,
  },
  helperText: {
    marginTop: spacing.xs,
  },
});
