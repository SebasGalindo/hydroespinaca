import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { spacing } from '@hidroespinaca/shared';
import { FormField } from './FormField';
import { OperatorChips, OperatorType } from '../atoms/OperatorChips';
import { Label } from '../atoms/Label';

export interface ValueFilterProps {
  /** Valor numérico actual */
  value: string;
  /** Callback cuando cambia el valor */
  onValueChange: (value: string) => void;
  /** Operador seleccionado */
  operator: OperatorType;
  /** Callback cuando cambia el operador */
  onOperatorChange: (operator: OperatorType) => void;
  /** Etiqueta del campo */
  label?: string;
  /** Placeholder del input */
  placeholder?: string;
  /** Texto de ayuda */
  helpText?: string;
  /** Mensaje de error */
  errorText?: string;
  /** Campo requerido */
  required?: boolean;
  /** Deshabilitar el componente */
  disabled?: boolean;
  /** Estado de carga */
  loading?: boolean;
  /** Estilo personalizado del contenedor */
  containerStyle?: ViewStyle;
  /** ID para testing */
  testID?: string;
}

export function ValueFilter({
  value,
  onValueChange,
  operator,
  onOperatorChange,
  label = 'Valor',
  placeholder = 'Ingrese valor',
  helpText,
  errorText,
  required = false,
  disabled = false,
  loading = false,
  containerStyle,
  testID = 'value-filter',
}: ValueFilterProps): React.ReactElement {
  return (
    <View style={[styles.container, containerStyle]} testID={testID}>
      {/* Campo de valor numérico */}
      <FormField
        label={label}
        value={value}
        onChangeText={onValueChange}
        placeholder={placeholder}
        {...(helpText && { helpText })}
        {...(errorText && { errorText })}
        required={required}
        disabled={disabled}
        loading={loading}
        inputProps={{
          keyboardType: 'numeric',
          returnKeyType: 'done',
        }}
        testID={`${testID}-input`}
      />

      {/* Operadores de comparación */}
      <View style={styles.operatorSection}>
        <Label style={styles.operatorLabel}>Operador</Label>
        <OperatorChips
          selectedOperator={operator}
          onOperatorChange={onOperatorChange}
          disabled={disabled}
          testID={`${testID}-operators`}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
  },
  operatorSection: {
    gap: spacing.sm,
  },
  operatorLabel: {
    // Reutiliza estilos del Label por defecto
  },
});