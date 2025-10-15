import React, { useState, useEffect } from 'react';
import { View, StyleSheet, ScrollView, ViewStyle } from 'react-native';
import { 
  semanticColors, 
  spacing, 
  typography, 
  useVariableStore
} from '@hydroespinaca/shared';
import type { IndividualSensorData } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { BottomSheet } from './BottomSheet';

export interface SensorBottomSheetProps {
  isVisible: boolean;
  onClose: () => void;
  onSave: (sensor: IndividualSensorData) => void;
  sensor?: IndividualSensorData | null;
  isEditMode: boolean;
}

const ESTADO_OPTIONS = [
  { label: 'Activo', value: 'activo' },
  { label: 'Inactivo', value: 'inactivo' },
];

export function SensorBottomSheet({
  isVisible,
  onClose,
  onSave,
  sensor,
  isEditMode
}: SensorBottomSheetProps): React.ReactElement {
  const { variables, initializeVariables } = useVariableStore();
  
  const [formData, setFormData] = useState<IndividualSensorData>({
    idFisico: '',
    ubicacion: '',
    esp32Id: '',
    frecuenciaLectura: 60, // 1 minuto por defecto
    variablesAMedir: [],
    unidadMedida: '',
    rangoMinimo: 0,
    rangoMaximo: 100,
    rangoOptimoMinimo: 20,
    rangoOptimoMaximo: 80,
    estado: 'activo',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Preparar opciones de variables para el MultiSelect
  const variableOptions = variables.map(variable => ({
    label: variable.name,
    value: variable.id,
  }));

  // Inicializar variables cuando el componente se monte
  useEffect(() => {
    if (variables.length === 0) {
      initializeVariables();
    }
  }, [variables.length, initializeVariables]);

  useEffect(() => {
    if (sensor && isEditMode) {
      setFormData(sensor);
    } else if (!isEditMode) {
      setFormData({
        idFisico: '',
        ubicacion: '',
        esp32Id: '',
        frecuenciaLectura: 60,
        variablesAMedir: [],
        unidadMedida: '',
        rangoMinimo: 0,
        rangoMaximo: 100,
        rangoOptimoMinimo: 20,
        rangoOptimoMaximo: 80,
        estado: 'activo',
      });
    }
    setErrors({});
  }, [sensor, isEditMode, isVisible]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.idFisico.trim()) {
      newErrors.idFisico = 'El ID físico es requerido';
    }

    if (!formData.ubicacion.trim()) {
      newErrors.ubicacion = 'La ubicación es requerida';
    }

    if (!formData.esp32Id.trim()) {
      newErrors.esp32Id = 'El ESP32 ID es requerido';
    }

    if (formData.frecuenciaLectura <= 0) {
      newErrors.frecuenciaLectura = 'La frecuencia debe ser mayor a 0';
    }

    if (formData.variablesAMedir.length === 0) {
      newErrors.variablesAMedir = 'Debe seleccionar al menos una variable';
    }

    if (!formData.unidadMedida.trim()) {
      newErrors.unidadMedida = 'La unidad de medida es requerida';
    }

    if (formData.rangoMinimo >= formData.rangoMaximo) {
      newErrors.rangoMinimo = 'El rango mínimo debe ser menor al máximo';
    }

    if (formData.rangoOptimoMinimo >= formData.rangoOptimoMaximo) {
      newErrors.rangoOptimoMinimo = 'El rango óptimo mínimo debe ser menor al máximo';
    }

    if (formData.rangoOptimoMinimo < formData.rangoMinimo || formData.rangoOptimoMaximo > formData.rangoMaximo) {
      newErrors.rangoOptimoMinimo = 'El rango óptimo debe estar dentro del rango general';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSave = () => {
    if (validateForm()) {
      const sensorData: IndividualSensorData = {
        ...formData,
        id: sensor?.id || `SENSOR-${Date.now()}`,
        createdAt: sensor?.createdAt || new Date().toISOString(),
        lastModified: new Date().toISOString(),
      };
      onSave(sensorData);
      onClose();
    }
  };

  const updateFormData = (field: keyof IndividualSensorData, value: string | number | string[]) => {
    setFormData((prev: IndividualSensorData) => ({ ...prev, [field]: value }));
    // Limpiar error del campo cuando el usuario empiece a escribir
    const fieldStr = String(field);
    if (errors[fieldStr]) {
      setErrors((prev: Record<string, string>) => ({ ...prev, [fieldStr]: '' }));
    }
  };

  return (
    <BottomSheet 
      isVisible={isVisible} 
      onClose={onClose}
      height={0.9} // 90% de la altura de la pantalla para acomodar todos los campos
    >
      <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text variant="h2" style={styles.title}>
            {!isEditMode ? 'Nuevo Sensor' : 'Editar Sensor'}
          </Text>
        </View>

        <ScrollView style={styles.formContainer} showsVerticalScrollIndicator={false}>
          {/* ID Físico del Sensor */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>ID Físico del Sensor *</Text>
            <Input
              value={formData.idFisico}
              onChangeText={(value: string) => updateFormData('idFisico', value)}
              placeholder="Ej: TEMP-001"
              error={!!errors.idFisico}
              style={styles.input}
            />
          </View>

          {/* Ubicación */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Ubicación *</Text>
            <Input
              value={formData.ubicacion}
              onChangeText={(value: string) => updateFormData('ubicacion', value)}
              placeholder="Ej: Zona A - Cultivo Principal"
              error={!!errors.ubicacion}
              style={styles.input}
            />
          </View>

          {/* ESP32 ID */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>ESP32 ID *</Text>
            <Input
              value={formData.esp32Id}
              onChangeText={(value: string) => updateFormData('esp32Id', value)}
              placeholder="Ej: ESP32-001"
              error={!!errors.esp32Id}
              style={styles.input}
            />
          </View>

          {/* Frecuencia de Lectura */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Frecuencia de Lectura (segundos) *</Text>
            <Input
              value={formData.frecuenciaLectura.toString()}
              onChangeText={(value: string) => updateFormData('frecuenciaLectura', parseInt(value) || 60)}
              placeholder="60"
              keyboardType="numeric"
              error={!!errors.frecuenciaLectura}
              style={styles.input}
            />
            <Text variant="caption" style={styles.helperText}>
              Tiempo entre lecturas en segundos (mínimo 10 segundos)
            </Text>
          </View>

          {/* Variables a Medir */}
           <View style={styles.fieldContainer}>
             <Text variant="label" style={styles.label}>Variable a Medir *</Text>
             <Select
               options={variableOptions}
               value={formData.variablesAMedir[0] || ''}
               onSelect={(option) => updateFormData('variablesAMedir', [option.value as string])}
               placeholder="Seleccionar variable"
               error={!!errors.variablesAMedir}
             />
           </View>

          {/* Unidad de Medida */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Unidad de Medida *</Text>
            <Input
              value={formData.unidadMedida}
              onChangeText={(value: string) => updateFormData('unidadMedida', value)}
              placeholder="Ej: °C, %, ppm"
              error={!!errors.unidadMedida}
              style={styles.input}
            />
          </View>

          {/* Rango Mínimo y Máximo */}
          <View style={styles.rowContainer}>
            <View style={[styles.fieldContainer, styles.halfWidth]}>
              <Text variant="label" style={styles.label}>Rango Mínimo *</Text>
              <Input
                value={formData.rangoMinimo.toString()}
                onChangeText={(value: string) => updateFormData('rangoMinimo', parseFloat(value) || 0)}
                placeholder="0"
                keyboardType="numeric"
                error={!!errors.rangoMinimo}
                style={styles.input}
              />
            </View>

            <View style={[styles.fieldContainer, styles.halfWidth]}>
              <Text variant="label" style={styles.label}>Rango Máximo *</Text>
              <Input
                value={formData.rangoMaximo.toString()}
                onChangeText={(value: string) => updateFormData('rangoMaximo', parseFloat(value) || 0)}
                placeholder="100"
                keyboardType="numeric"
                error={!!errors.rangoMaximo}
                style={styles.input}
              />
            </View>
          </View>

          {/* Rango Óptimo Mínimo y Máximo */}
          <View style={styles.rowContainer}>
            <View style={[styles.fieldContainer, styles.halfWidth]}>
              <Text variant="label" style={styles.label}>Rango Óptimo Mín *</Text>
              <Input
                value={formData.rangoOptimoMinimo.toString()}
                onChangeText={(value: string) => updateFormData('rangoOptimoMinimo', parseFloat(value) || 0)}
                placeholder="20"
                keyboardType="numeric"
                error={!!errors.rangoOptimoMinimo}
                style={styles.input}
              />
            </View>

            <View style={[styles.fieldContainer, styles.halfWidth]}>
              <Text variant="label" style={styles.label}>Rango Óptimo Máx *</Text>
              <Input
                value={formData.rangoOptimoMaximo.toString()}
                onChangeText={(value: string) => updateFormData('rangoOptimoMaximo', parseFloat(value) || 0)}
                placeholder="80"
                keyboardType="numeric"
                error={!!errors.rangoOptimoMaximo}
                style={styles.input}
              />
            </View>
          </View>

          {/* Estado */}
           <View style={styles.fieldContainer}>
             <Text variant="label" style={styles.label}>Estado *</Text>
             <Select
               options={ESTADO_OPTIONS}
               value={formData.estado}
               onSelect={(option) => updateFormData('estado', option.value as string)}
               placeholder="Seleccionar estado"
               error={!!errors.estado}
             />
           </View>
        </ScrollView>

        {/* Action Buttons */}
        <View style={styles.actionButtons}>
          <Button
            variant="secondary"
            onPress={onClose}
            style={[styles.button, styles.cancelButton]}
          >
            Cancelar
          </Button>
          <Button
            variant="primary"
            onPress={handleSave}
            style={[styles.button, styles.saveButton]}
          >
            {!isEditMode ? 'Crear Sensor' : 'Guardar Cambios'}
          </Button>
        </View>
      </View>
    </BottomSheet>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl,
  },

  header: {
    paddingVertical: spacing.lg,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
    marginBottom: spacing.lg,
  },

  title: {
    fontSize: typography.fontSize.xl,
    fontWeight: 'bold' as const,
    color: semanticColors.textPrimary,
    textAlign: 'center',
  },

  formContainer: {
    flex: 1,
    paddingVertical: spacing.md,
  },

  fieldContainer: {
    marginBottom: spacing.lg,
  },

  rowContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: spacing.md,
  },

  halfWidth: {
    flex: 1,
  },

  label: {
    fontSize: typography.fontSize.sm,
    fontWeight: '500' as const,
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },

  input: {
     marginBottom: spacing.xs,
   },

   helperText: {
     fontSize: typography.fontSize.xs,
     color: semanticColors.textSecondary,
     marginTop: spacing.xs,
   },

   actionButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: spacing.md,
    paddingTop: spacing.lg,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
  },

  button: {
    flex: 1,
  },

  cancelButton: {
    flex: 1,
    backgroundColor: semanticColors.backgroundSecondary,
  } as ViewStyle,

  saveButton: {
    flex: 1,
    backgroundColor: semanticColors.primary,
  } as ViewStyle,
});