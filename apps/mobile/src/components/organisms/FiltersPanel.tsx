import React, { useState, useCallback } from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { semanticColors, spacing, borderRadius } from '@hidroespinaca/shared';
import { Text, Button, Label } from '../atoms';
import { DatePicker, Card } from '../molecules';
import { TimePicker, TimeValue } from '../molecules/TimePicker';
import { ValueFilter } from '../molecules/ValueFilter';
import { Select } from '../atoms/Select';
import { OperatorType } from '../atoms/OperatorChips';

export interface FilterValues {
  date?: Date;
  time?: TimeValue;
  sensor: string;
  numericValue: string;
  operator: OperatorType;
}

export interface FiltersPanelProps {
  /** Valores actuales de los filtros */
  values: FilterValues;
  /** Callback cuando cambian los filtros */
  onFiltersChange: (filters: FilterValues) => void;
  /** Opciones disponibles para el selector de sensor */
  sensorOptions: Array<{ label: string; value: string }>;
  /** Callback para limpiar todos los filtros */
  onClearFilters?: () => void;
  /** Mostrar botón de limpiar filtros */
  showClearButton?: boolean;
  /** Deshabilitar todos los filtros */
  disabled?: boolean;
  /** Estado de carga */
  loading?: boolean;
  /** Estilo personalizado del contenedor */
  containerStyle?: ViewStyle;
  /** ID para testing */
  testID?: string;
}

export function FiltersPanel({
  values,
  onFiltersChange,
  sensorOptions,
  onClearFilters,
  showClearButton = true,
  disabled = false,
  loading = false,
  containerStyle,
  testID = 'filters-panel',
}: FiltersPanelProps): React.ReactElement {
  // Handlers para cada filtro
  const handleDateChange = useCallback((date?: Date) => {
    if (date) {
      onFiltersChange({ ...values, date });
    } else {
      const { date: _, ...restValues } = values;
      onFiltersChange(restValues);
    }
  }, [values, onFiltersChange]);

  const handleTimeChange = useCallback((timeValue?: TimeValue) => {
    if (timeValue) {
      onFiltersChange({ ...values, time: timeValue });
    } else {
      const { time, ...restValues } = values;
      onFiltersChange(restValues);
    }
  }, [values, onFiltersChange]);

  const handleSensorChange = useCallback((option: { label: string; value: string | number }) => {
    onFiltersChange({ ...values, sensor: String(option.value) });
  }, [values, onFiltersChange]);

  const handleValueChange = useCallback((numericValue: string) => {
    onFiltersChange({ ...values, numericValue });
  }, [values, onFiltersChange]);

  const handleOperatorChange = useCallback((operator: OperatorType) => {
    onFiltersChange({ ...values, operator });
  }, [values, onFiltersChange]);

  const handleClearFilters = useCallback(() => {
    const clearedFilters: FilterValues = {
      sensor: 'Todos',
      numericValue: '',
      operator: 'eq',
    };
    onFiltersChange(clearedFilters);
    onClearFilters?.();
  }, [onFiltersChange, onClearFilters]);

  // Verificar si hay filtros activos
  const hasActiveFilters = values.date || values.time || 
    (values.sensor !== 'Todos') || values.numericValue.trim() !== '';

  return (
    <Card elevated={true} style={StyleSheet.flatten([styles.container, containerStyle])} testID={testID}>
      {/* Encabezado con título y botón de limpiar */}
      <View style={styles.header}>
        <Text variant="h3" style={styles.title}>
          Filtros
        </Text>
        {showClearButton && hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onPress={handleClearFilters}
            disabled={disabled || loading}
            testID={`${testID}-clear-button`}
          >
            Limpiar
          </Button>
        )}
      </View>

      {/* Filtros */}
      <View style={styles.filtersContainer}>
        {/* Fecha */}
        <DatePicker
          label="Fecha"
          {...(values.date && { value: values.date })}
          onDateChange={handleDateChange}
          placeholder="Seleccionar fecha"
          icon="calendar"
          disabled={disabled || loading}
          containerStyle={styles.filterField}
        />

        {/* Hora */}
        <TimePicker
          label="Hora"
          {...(values.time && { value: values.time })}
          onConfirm={handleTimeChange}
          placeholder="Seleccionar hora"
          icon="clock"
          disabled={disabled}
          style={styles.filterField}
        />

        {/* Sensor */}
        <View style={styles.filterField}>
          <Label>Sensor</Label>
          <Select
            options={sensorOptions}
            value={values.sensor}
            placeholder="Todos"
            onSelect={handleSensorChange}
            disabled={disabled || loading}
            testID={`${testID}-sensor`}
          />
        </View>

        {/* Valor con operadores */}
        <ValueFilter
          value={values.numericValue}
          onValueChange={handleValueChange}
          operator={values.operator}
          onOperatorChange={handleOperatorChange}
          label="Valor"
          placeholder="Ingrese valor"
          disabled={disabled || loading}
          containerStyle={styles.filterField}
          testID={`${testID}-value`}
        />
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.md,
    // marginHorizontal removed to rely on parent padding
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    color: semanticColors.textPrimary,
  },
  filtersContainer: {
    gap: spacing.md,
  },
  filterField: {
    width: '100%',
  },
});