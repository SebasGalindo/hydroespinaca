import React from 'react';
import { View, ViewStyle, TextStyle } from 'react-native';
import { spacing, semanticColors } from '@hidroespinaca/shared';
import { Label, Input, Spinner, Text } from '../atoms';

export interface FormFieldProps {
  label?: string;
  value?: string;
  onChangeText?: (text: string) => void;
  placeholder?: string;
  required?: boolean;
  helpText?: string;
  errorText?: string;
  loading?: boolean;
  disabled?: boolean;
  inputProps?: React.ComponentProps<typeof Input>;
  containerStyle?: ViewStyle;
  labelStyle?: TextStyle;
  testID?: string;
}

export function FormField({
  label,
  value,
  onChangeText,
  placeholder,
  required = false,
  helpText,
  errorText,
  loading = false,
  disabled = false,
  inputProps,
  containerStyle,
  labelStyle,
  testID,
}: FormFieldProps): React.ReactElement {
  const hasError = Boolean(errorText || inputProps?.error);

  return (
    <View style={[{ width: '100%' }, containerStyle]} testID={testID}>
      {label ? (
        <Label 
          required={required} 
          disabled={!!disabled} 
          error={hasError} 
          {...(labelStyle && { style: labelStyle })}
        >
          {label}
        </Label>
      ) : null}

      <Input
        {...inputProps}
        value={value || inputProps?.value}
        onChangeText={onChangeText || inputProps?.onChangeText}
        placeholder={placeholder || inputProps?.placeholder}
        disabled={!!disabled || !!inputProps?.disabled}
        error={hasError}
        rightIcon={
          loading ? (
            <Spinner size="sm" color={semanticColors.primary} />
          ) : (
            inputProps?.rightIcon
          )
        }
      />

      {/* Mensajes auxiliares */}
      {hasError ? (
        <Text variant="body" style={{ color: semanticColors.errorText, marginTop: spacing.xs }}>
          {errorText}
        </Text>
      ) : helpText ? (
        <Text variant="body" style={{ color: semanticColors.textMuted, marginTop: spacing.xs }}>
          {helpText}
        </Text>
      ) : null}
    </View>
  );
}