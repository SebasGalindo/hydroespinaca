import React, { useState, useEffect } from 'react';
import { View, StyleSheet, ScrollView, ViewStyle } from 'react-native';
import { 
  semanticColors, 
  spacing, 
  typography 
} from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';
import { BottomSheet } from './BottomSheet';

export interface ActuadorData {
  id?: string;
  nombre: string;
  tipoActuador: 'heater' | 'fan' | 'led' | 'pump';
  ubicacion: string;
  esp32Id: string;
  pinEsp32: number;
  status: 'active' | 'inactive';
  createdAt?: string;
  lastModified?: string;
}

export interface ActuadorBottomSheetProps {
  isVisible: boolean;
  onClose: () => void;
  onSave: (actuador: ActuadorData) => void;
  actuador?: ActuadorData | null;
  isEditMode: boolean;
}

const TIPO_ACTUADOR_OPTIONS = [
  { label: 'Calentador', value: 'heater' },
  { label: 'Ventilador', value: 'fan' },
  { label: 'LED', value: 'led' },
  { label: 'Bomba', value: 'pump' },
];

const STATUS_OPTIONS = [
  { label: 'Activo', value: 'active' },
  { label: 'Inactivo', value: 'inactive' },
];

export function ActuadorBottomSheet({
  isVisible,
  onClose,
  onSave,
  actuador,
  isEditMode
}: ActuadorBottomSheetProps): React.ReactElement {
  
  const [formData, setFormData] = useState<ActuadorData>({
    nombre: '',
    tipoActuador: 'pump',
    ubicacion: '',
    esp32Id: '',
    pinEsp32: 0,
    status: 'active',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (actuador && isEditMode) {
      setFormData(actuador);
    } else if (!isEditMode) {
      setFormData({
        nombre: '',
        tipoActuador: 'pump',
        ubicacion: '',
        esp32Id: '',
        pinEsp32: 0,
        status: 'active',
      });
    }
    setErrors({});
  }, [actuador, isEditMode, isVisible]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.nombre.trim()) {
      newErrors.nombre = 'El nombre es requerido';
    }

    if (!formData.ubicacion.trim()) {
      newErrors.ubicacion = 'La ubicación es requerida';
    }

    if (!formData.esp32Id.trim()) {
      newErrors.esp32Id = 'El ESP32 ID es requerido';
    }

    if (formData.pinEsp32 < 0 || formData.pinEsp32 > 39) {
      newErrors.pinEsp32 = 'El PIN debe estar entre 0 y 39';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSave = () => {
    if (validateForm()) {
      const actuadorData: ActuadorData = {
        ...formData,
        id: actuador?.id || `ACT-${Date.now()}`,
        createdAt: actuador?.createdAt || new Date().toISOString(),
        lastModified: new Date().toISOString(),
      };
      onSave(actuadorData);
      onClose();
    }
  };

  const updateFormData = (field: keyof ActuadorData, value: string | number) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Limpiar error del campo cuando el usuario empiece a escribir
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <BottomSheet 
      isVisible={isVisible} 
      onClose={onClose}
      height={0.8} // 80% de la altura de la pantalla
    >
      <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text variant="h2" style={styles.title}>
            {!isEditMode ? 'Nuevo Actuador' : 'Editar Actuador'}
          </Text>
        </View>

        <ScrollView style={styles.formContainer} showsVerticalScrollIndicator={false}>
          {/* Nombre */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Nombre *</Text>
            <Input
              value={formData.nombre}
              onChangeText={(value: string) => updateFormData('nombre', value)}
              placeholder="Ej: Bomba Principal"
              error={!!errors.nombre}
              style={styles.input}
            />
          </View>

          {/* Tipo de Actuador */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Tipo de Actuador *</Text>
            <Select
              options={TIPO_ACTUADOR_OPTIONS}
              value={formData.tipoActuador}
              onSelect={(option) => updateFormData('tipoActuador', option.value)}
              placeholder="Seleccionar tipo"
              error={!!errors.tipoActuador}
            />
          </View>

          {/* Ubicación */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Ubicación *</Text>
            <Input
              value={formData.ubicacion}
              onChangeText={(value: string) => updateFormData('ubicacion', value)}
              placeholder="Ej: Zona A - Sistema de Riego"
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

          {/* PIN ESP32 */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>PIN ESP32 *</Text>
            <Input
              value={formData.pinEsp32.toString()}
              onChangeText={(value: string) => updateFormData('pinEsp32', parseInt(value) || 0)}
              placeholder="Ej: 2"
              keyboardType="numeric"
              error={!!errors.pinEsp32}
              style={styles.input}
            />
            <Text variant="caption" style={styles.helperText}>
              PIN GPIO del ESP32 (0-39)
            </Text>
          </View>

          {/* Status */}
          <View style={styles.fieldContainer}>
            <Text variant="label" style={styles.label}>Estado *</Text>
            <Select
              options={STATUS_OPTIONS}
              value={formData.status}
              onSelect={(option) => updateFormData('status', option.value)}
              placeholder="Seleccionar estado"
              error={!!errors.status}
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
            {!isEditMode ? 'Crear Actuador' : 'Guardar Cambios'}
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