import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, typography, VariableData } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { Button } from '../atoms/Button';

export interface VariableFormData {
  name: string;
  description: string;
  type: 'input' | 'output' | 'calculated';
  dataType: 'numeric' | 'boolean' | 'text';
  unit: string;
  category: 'environmental' | 'control' | 'system' | 'user';
  minValue: string;
  maxValue: string;
}

export interface VariableFormProps {
  /** Datos iniciales del formulario (para edición) */
  initialData?: Partial<VariableFormData> | undefined;
  /** Función llamada cuando se envía el formulario */
  onSubmit: (data: VariableFormData) => void;
  /** Función llamada cuando se cancela */
  onCancel: () => void;
  /** Si está en modo edición */
  isEditing?: boolean;
  /** Referencia para controlar el formulario externamente */
  ref?: React.Ref<{ submit: () => void; isValid: () => boolean }>;
}

export function VariableForm({
  initialData,
  onSubmit,
  onCancel,
  isEditing = false,
}: VariableFormProps): React.ReactElement {
  
  const [formData, setFormData] = useState<VariableFormData>({
    name: '',
    description: '',
    type: 'input',
    dataType: 'numeric',
    unit: '',
    category: 'environmental',
    minValue: '',
    maxValue: '',
  });

  const [errors, setErrors] = useState<Partial<Record<keyof VariableFormData, string>>>({});

  // Cargar datos iniciales si están disponibles
  useEffect(() => {
    if (initialData) {
      setFormData(prev => ({
        ...prev,
        ...initialData,
      }));
    }
  }, [initialData]);

  // Opciones para los selects
  const typeOptions = [
    { label: 'Entrada', value: 'input' },
    { label: 'Salida', value: 'output' },
    { label: 'Calculada', value: 'calculated' },
  ];

  const dataTypeOptions = [
    { label: 'Numérico', value: 'numeric' },
    { label: 'Booleano', value: 'boolean' },
    { label: 'Texto', value: 'text' },
  ];

  const categoryOptions = [
    { label: 'Ambiental', value: 'environmental' },
    { label: 'Control', value: 'control' },
    { label: 'Sistema', value: 'system' },
    { label: 'Usuario', value: 'user' },
  ];

  // Función para actualizar un campo del formulario
  const updateField = <K extends keyof VariableFormData>(field: K, value: VariableFormData[K]) => {
    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));

    // Limpiar error del campo si existe
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: undefined,
      }));
    }
  };

  // Validación del formulario
  const validateForm = (): boolean => {
    const newErrors: Partial<Record<keyof VariableFormData, string>> = {};

    // Validar nombre
    if (!formData.name.trim()) {
      newErrors.name = 'El nombre es requerido';
    }

    // Validar descripción
    if (!formData.description.trim()) {
      newErrors.description = 'La descripción es requerida';
    }

    // Validar unidad de medida
    if (!formData.unit.trim()) {
      newErrors.unit = 'La unidad de medida es requerida';
    }

    // Validar rango de valores para tipo numérico
    if (formData.dataType === 'numeric') {
      const minValue = parseFloat(formData.minValue);
      const maxValue = parseFloat(formData.maxValue);

      if (formData.minValue === '' || isNaN(minValue)) {
        newErrors.minValue = 'El valor mínimo es requerido';
      }

      if (formData.maxValue === '' || isNaN(maxValue)) {
        newErrors.maxValue = 'El valor máximo es requerido';
      }

      if (!isNaN(minValue) && !isNaN(maxValue) && minValue >= maxValue) {
        newErrors.maxValue = 'El valor máximo debe ser mayor al mínimo';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Manejar envío del formulario
  const handleSubmit = () => {
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <View style={styles.container}>
      {/* Nombre de la Variable */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Nombre de la Variable</Text>
        <Input
          placeholder="Ej: Temperatura del agua"
          value={formData.name}
          onChangeText={(text) => updateField('name', text)}
          style={errors.name ? styles.inputError : {}}
        />
        {errors.name && (
          <Text style={styles.errorText}>{errors.name}</Text>
        )}
      </View>

      {/* Descripción */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Descripción</Text>
        <Input
          placeholder="Describe el propósito de esta variable"
          value={formData.description}
          onChangeText={(text) => updateField('description', text)}
          multiline
          numberOfLines={3}
          style={{
            ...styles.textArea,
            ...(errors.description ? styles.inputError : {})
          }}
        />
        {errors.description && (
          <Text style={styles.errorText}>{errors.description}</Text>
        )}
      </View>

      {/* Tipo de Variable */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Tipo de Variable</Text>
        <Select
          options={typeOptions}
          value={formData.type}
          onSelect={(option) => updateField('type', option.value as VariableFormData['type'])}
          placeholder="Selecciona el tipo"
        />
      </View>

      {/* Tipo de Datos */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Tipo de Datos</Text>
        <Select
          options={dataTypeOptions}
          value={formData.dataType}
          onSelect={(option) => updateField('dataType', option.value as VariableFormData['dataType'])}
          placeholder="Selecciona el tipo de dato"
        />
      </View>

      {/* Unidad de Medida */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Unidad de Medida</Text>
        <Input
          placeholder="Ej: °C, pH, ppm"
          value={formData.unit}
          onChangeText={(text) => updateField('unit', text)}
          style={errors.unit ? styles.inputError : {}}
        />
        {errors.unit && (
          <Text style={styles.errorText}>{errors.unit}</Text>
        )}
      </View>

      {/* Categoría */}
      <View style={styles.fieldContainer}>
        <Text style={styles.fieldLabel}>Categoría</Text>
        <Select
          options={categoryOptions}
          value={formData.category}
          onSelect={(option) => updateField('category', option.value as VariableFormData['category'])}
          placeholder="Selecciona la categoría"
        />
      </View>

      {/* Rango de Valores - Solo para tipo numérico */}
      {formData.dataType === 'numeric' && (
        <View style={styles.rangeContainer}>
          <Text style={styles.fieldLabel}>Rango de Valores</Text>
          <View style={styles.rangeInputs}>
            <View style={styles.rangeInput}>
              <Text style={styles.rangeLabel}>Mínimo</Text>
              <Input
                placeholder="0"
                value={formData.minValue}
                onChangeText={(text) => updateField('minValue', text)}
                keyboardType="numeric"
                style={errors.minValue ? styles.inputError : {}}
              />
              {errors.minValue && (
                <Text style={styles.errorText}>{errors.minValue}</Text>
              )}
            </View>
            
            <View style={styles.rangeInput}>
              <Text style={styles.rangeLabel}>Máximo</Text>
              <Input
                placeholder="100"
                value={formData.maxValue}
                onChangeText={(text) => updateField('maxValue', text)}
                keyboardType="numeric"
                style={errors.maxValue ? styles.inputError : {}}
              />
              {errors.maxValue && (
                <Text style={styles.errorText}>{errors.maxValue}</Text>
              )}
            </View>
          </View>
        </View>
      )}

      {/* Actions */}
      <View style={styles.actions}>
        <Button
          variant="secondary"
          onPress={onCancel}
          style={styles.cancelButton}
        >
          Cancelar
        </Button>
        
        <Button
          variant="primary"
          onPress={handleSubmit}
          style={styles.confirmButton}
        >
          {isEditing ? 'Actualizar Variable' : 'Crear Variable'}
        </Button>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: spacing.lg,
  },

  fieldContainer: {
    marginBottom: spacing.md,
  },

  fieldLabel: {
    fontSize: typography.fontSize.sm,
    fontWeight: typography.fontWeight.medium as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },

  textArea: {
    minHeight: 80,
    textAlignVertical: 'top',
  },

  rangeContainer: {
    marginBottom: spacing.md,
  },

  rangeInputs: {
    flexDirection: 'row',
    gap: spacing.md,
    marginTop: spacing.xs,
  },

  rangeInput: {
    flex: 1,
  },

  rangeLabel: {
    fontSize: typography.fontSize.xs,
    fontWeight: typography.fontWeight.medium as any,
    color: semanticColors.textSecondary,
    marginBottom: spacing.xs,
  },

  inputError: {
    borderColor: semanticColors.errorText,
    borderWidth: 1,
  },

  errorText: {
    fontSize: typography.fontSize.xs,
    color: semanticColors.errorText,
    marginTop: spacing.xs,
  },

  actions: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingTop: spacing.xl,
    marginTop: spacing.lg,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },

  cancelButton: {
    flex: 1,
  },

  confirmButton: {
    flex: 1,
  },
});