import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { spacing } from '@hydroespinaca/shared';
import { Button } from './Button';

export type OperatorType = 'gt' | 'gte' | 'eq' | 'lte' | 'lt';

export interface OperatorChipsProps {
  /** Operador seleccionado actualmente */
  selectedOperator: OperatorType;
  /** Callback cuando se selecciona un operador */
  onOperatorChange: (operator: OperatorType) => void;
  /** Tamaño de los chips */
  size?: 'sm' | 'md' | 'lg';
  /** Estilo personalizado del contenedor */
  style?: ViewStyle;
  /** Deshabilitar todos los chips */
  disabled?: boolean;
  /** ID para testing */
  testID?: string;
}

const OPERATORS: Array<{ key: OperatorType; label: string; description: string }> = [
  { key: 'gt', label: '>', description: 'Mayor que' },
  { key: 'gte', label: '>=', description: 'Mayor o igual que' },
  { key: 'eq', label: '=', description: 'Igual a' },
  { key: 'lte', label: '<=', description: 'Menor o igual que' },
  { key: 'lt', label: '<', description: 'Menor que' },
];

export function OperatorChips({
  selectedOperator,
  onOperatorChange,
  size = 'sm',
  style,
  disabled = false,
  testID = 'operator-chips',
}: OperatorChipsProps): React.ReactElement {
  return (
    <View style={[styles.container, style]} testID={testID}>
      {OPERATORS.map((operator) => (
        <Button
          key={operator.key}
          variant={selectedOperator === operator.key ? 'primary' : 'outline'}
          size={size}
          onPress={() => onOperatorChange(operator.key)}
          disabled={disabled}
          style={styles.chip}
          testID={`${testID}-${operator.key}`}
          accessibilityLabel={`${operator.description}: ${operator.label}`}
        >
          {operator.label}
        </Button>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    gap: spacing.sm,
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  chip: {
    minWidth: 48,
    paddingHorizontal: spacing.sm,
  },
});