import React, { useState } from 'react';
import { View, StyleSheet } from 'react-native';
import { Button, Text, Select } from '../atoms';
import { FormField } from '../molecules';
import { BottomSheet } from './BottomSheet';
import { RangeInputGroup, NotificationConfig } from '../molecules';
import { colors, semanticColors, spacing, typography } from '@hydroespinaca/shared';

export interface VariableFormData {
  name: string;
  unit: string;
  description: string;
  type: string;
  generalMinValue: string;
  generalMaxValue: string;
  optimalMinValue: string;
  optimalMaxValue: string;
  notificationsEnabled: boolean;
  frequency: string;
  startDate: string;
  endDate: string;
  reminderTime: string;
}

export interface NewVariableFormProps {
  /** Si el formulario está visible */
  visible: boolean;
  /** Callback para cerrar el formulario */
  onClose: () => void;
  /** Callback para guardar la variable */
  onSave: (data: VariableFormData) => void;
  /** Datos iniciales del formulario */
  initialData?: Partial<VariableFormData>;
  /** Si está en modo de carga */
  loading?: boolean;
}

const variableTypeOptions = [
  { 
    label: 'Ambiental', 
    value: 'environmental'
  },
  { 
    label: 'Nutricional', 
    value: 'nutritional'
  },
  { 
    label: 'Física', 
    value: 'physical'
  },
  { 
    label: 'Biológica', 
    value: 'biological'
  },
];

export const NewVariableForm: React.FC<NewVariableFormProps> = ({
  visible,
  onClose,
  onSave,
  initialData = {},
  loading = false,
}) => {
  // Estados del formulario
  const [formData, setFormData] = useState<VariableFormData>({
    name: initialData.name || '',
    unit: initialData.unit || '',
    description: initialData.description || '',
    type: initialData.type || '',
    generalMinValue: initialData.generalMinValue || '',
    generalMaxValue: initialData.generalMaxValue || '',
    optimalMinValue: initialData.optimalMinValue || '',
    optimalMaxValue: initialData.optimalMaxValue || '',
    notificationsEnabled: initialData.notificationsEnabled || false,
    frequency: initialData.frequency || 'daily',
    startDate: initialData.startDate || '',
    endDate: initialData.endDate || '',
    reminderTime: initialData.reminderTime || '09:00',
  });

  const [errors, setErrors] = useState<Partial<Record<keyof VariableFormData, string>>>({});

  // Validaciones
  const validateForm = (): boolean => {
    const newErrors: Partial<Record<keyof VariableFormData, string>> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'El nombre es requerido';
    }

    if (!formData.unit.trim()) {
      newErrors.unit = 'La unidad de medida es requerida';
    }

    if (!formData.type) {
      newErrors.type = 'El tipo de variable es requerido';
    }

    if (!formData.generalMinValue.trim()) {
      newErrors.generalMinValue = 'El valor mínimo general es requerido';
    }

    if (!formData.generalMaxValue.trim()) {
      newErrors.generalMaxValue = 'El valor máximo general es requerido';
    }

    if (formData.generalMinValue && formData.generalMaxValue) {
      const minVal = parseFloat(formData.generalMinValue);
      const maxVal = parseFloat(formData.generalMaxValue);
      if (minVal >= maxVal) {
        newErrors.generalMaxValue = 'El valor máximo debe ser mayor al mínimo';
      }
    }

    if (formData.notificationsEnabled) {
      if (!formData.frequency) {
        newErrors.frequency = 'La frecuencia es requerida';
      }
      if (!formData.startDate) {
        newErrors.startDate = 'La fecha de inicio es requerida';
      }
      if (!formData.reminderTime) {
        newErrors.reminderTime = 'La hora de recordatorio es requerida';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handlers
  const handleInputChange = (field: keyof VariableFormData, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));
    
    // Limpiar error del campo cuando el usuario empiece a escribir
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: undefined,
      }));
    }
  };

  const handleRangeChange = (
    type: 'general' | 'optimal',
    minValue: string,
    maxValue: string
  ) => {
    if (type === 'general') {
      setFormData(prev => ({
        ...prev,
        generalMinValue: minValue,
        generalMaxValue: maxValue,
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        optimalMinValue: minValue,
        optimalMaxValue: maxValue,
      }));
    }
  };

  const handleSave = () => {
    if (validateForm()) {
      onSave(formData);
    }
  };

  const handleCancel = () => {
    setFormData({
      name: '',
      unit: '',
      description: '',
      type: '',
      generalMinValue: '',
      generalMaxValue: '',
      optimalMinValue: '',
      optimalMaxValue: '',
      notificationsEnabled: false,
      frequency: 'daily',
      startDate: '',
      endDate: '',
      reminderTime: '09:00',
    });
    setErrors({});
    onClose();
  };

  return (
    <BottomSheet isVisible={visible} onClose={handleCancel} height="full" scrollable={true}>
      <View style={styles.container}>
        <Text style={styles.title}>Nueva Variable Manual</Text>
        <Text style={styles.subtitle}>
          Define los parámetros para la variable que será controlada manualmente
        </Text>
          {/* Información Básica */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Información Básica</Text>
            
            <FormField
              label="Nombre de la Variable *"
              value={formData.name}
              onChangeText={(value: string) => handleInputChange('name', value)}
              placeholder="Ej: Nivel de Nutrientes"
              {...(errors.name && { errorText: errors.name })}
              disabled={loading}
            />

            <FormField
              label="Unidad de Medida *"
              value={formData.unit}
              onChangeText={(value: string) => handleInputChange('unit', value)}
              placeholder="Ej: mg/L, cm, %"
              {...(errors.unit && { errorText: errors.unit })}
              disabled={loading}
            />

            <FormField
              label="Descripción"
              value={formData.description}
              onChangeText={(value: string) => handleInputChange('description', value)}
              placeholder="Describe brevemente qué mide esta variable y su importancia"
              inputProps={{ multiline: true, numberOfLines: 3 }}
              disabled={loading}
            />
          </View>

          {/* Tipo de Variable */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Tipo de Variable</Text>
            
            <Select
              options={variableTypeOptions}
              value={formData.type}
              onSelect={(option) => handleInputChange('type', option.value as string)}
              placeholder="Selecciona el tipo de variable"
              error={!!errors.type}
              disabled={loading}
            />
          </View>

          {/* Rangos de Valores */}
          <View style={styles.section}>
            <RangeInputGroup
              title="Rangos de Valores"
              generalMinValue={formData.generalMinValue}
              generalMaxValue={formData.generalMaxValue}
              optimalMinValue={formData.optimalMinValue}
              optimalMaxValue={formData.optimalMaxValue}
              onGeneralRangeChange={(min, max) => handleRangeChange('general', min, max)}
              onOptimalRangeChange={(min, max) => handleRangeChange('optimal', min, max)}
              unit={formData.unit}
              disabled={loading}
              errors={{
                generalMin: errors.generalMinValue || undefined,
                generalMax: errors.generalMaxValue || undefined,
                optimalMin: errors.optimalMinValue || undefined,
                optimalMax: errors.optimalMaxValue || undefined,
              }}
            />
          </View>

          {/* Configuración de Notificaciones */}
          <View style={styles.section}>
            <NotificationConfig
              enabled={formData.notificationsEnabled}
              frequency={formData.frequency}
              startDate={formData.startDate}
              endDate={formData.endDate}
              reminderTime={formData.reminderTime}
              onEnabledChange={(enabled) => handleInputChange('notificationsEnabled', enabled)}
              onFrequencyChange={(frequency) => handleInputChange('frequency', frequency)}
              onStartDateChange={(date) => handleInputChange('startDate', date)}
              onEndDateChange={(date) => handleInputChange('endDate', date)}
              onReminderTimeChange={(time) => handleInputChange('reminderTime', time)}
              disabled={loading}
              errors={{
                frequency: errors.frequency || undefined,
                startDate: errors.startDate || undefined,
                endDate: errors.endDate || undefined,
                reminderTime: errors.reminderTime || undefined,
              }}
            />
          </View>

        {/* Botones de acción */}
        <View style={styles.buttonContainer}>
          <Button
            variant="outline"
            onPress={handleCancel}
            disabled={loading}
            style={styles.cancelButton}
          >
            Cancelar
          </Button>
          <Button
            variant="primary"
            onPress={handleSave}
            loading={loading}
            style={styles.saveButton}
          >
            Guardar Variable
          </Button>
        </View>
      </View>
    </BottomSheet>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingHorizontal: spacing.md,
  },
  title: {
    fontSize: typography.fontSize.xl,
    fontWeight: typography.fontWeight.bold as any,
    color: semanticColors.textPrimary,
    textAlign: 'center',
    marginBottom: spacing.xs,
  },
  subtitle: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
    textAlign: 'center',
    marginBottom: spacing.lg,
    lineHeight: 18,
  },
  section: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    fontSize: typography.fontSize.lg,
    fontWeight: typography.fontWeight.semibold as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  buttonContainer: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingTop: spacing.md,
    paddingBottom: spacing.lg,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
    marginTop: spacing.lg,
  },
  cancelButton: {
    flex: 1,
  },
  saveButton: {
    flex: 1,
  },
});

export default NewVariableForm;