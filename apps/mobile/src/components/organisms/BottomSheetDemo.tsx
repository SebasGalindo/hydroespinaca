import React, { useState } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { semanticColors, typography, spacing } from '@hidroespinaca/shared';
import { BottomSheet } from './BottomSheet';
import { Button } from '../atoms/Button';
import { Input } from '../atoms/Input';
import { Select } from '../atoms/Select';

export function BottomSheetDemo(): React.ReactElement {
  const [isVariableFormVisible, setIsVariableFormVisible] = useState(false);
  const [isConfirmVisible, setIsConfirmVisible] = useState(false);
  
  // Estado del formulario
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    type: 'input',
    unit: '',
    minValue: '',
    maxValue: '',
  });

  const handleSaveVariable = () => {
    // Aquí iría la lógica de guardado
    console.log('Guardando variable:', formData);
    setIsVariableFormVisible(false);
    // Reset form
    setFormData({
      name: '',
      description: '',
      type: 'input',
      unit: '',
      minValue: '',
      maxValue: '',
    });
  };

  const handleDeleteConfirm = () => {
    // Aquí iría la lógica de eliminación
    console.log('Eliminando elemento');
    setIsConfirmVisible(false);
  };

  const variableTypeOptions = [
    { label: 'Entrada (Input)', value: 'input' },
    { label: 'Salida (Output)', value: 'output' },
    { label: 'Calculada', value: 'calculated' },
  ];

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Bottom Sheet Demo</Text>
      <Text style={styles.subtitle}>
        Ejemplos de uso del BottomSheet para formularios móviles
      </Text>

      <View style={styles.buttonContainer}>
        <Button
          onPress={() => setIsVariableFormVisible(true)}
          variant="primary"
          style={styles.button}
        >
          Crear Nueva Variable
        </Button>
        
        <Button
          children="Confirmar Eliminación"
          onPress={() => setIsConfirmVisible(true)}
          variant="danger"
          style={styles.button}
        />
      </View>

      {/* Bottom Sheet para Formulario de Variable */}
      <BottomSheet
        isVisible={isVariableFormVisible}
        onClose={() => setIsVariableFormVisible(false)}
        title="Nueva Variable"
        height="full"
        footer={
          <View style={styles.footerButtons}>
            <Button
              onPress={() => setIsVariableFormVisible(false)}
              variant="secondary"
              style={styles.footerButton}
            >
              Cancelar
            </Button>
            <Button
              onPress={handleSaveVariable}
              variant="primary"
              style={styles.footerButton}
            >
              Guardar
            </Button>
          </View>
        }
      >
        <View style={styles.formContainer}>
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Nombre de la Variable</Text>
            <Input
              placeholder="Ej: Temperatura Ambiente"
              value={formData.name}
              onChangeText={(text) => setFormData({ ...formData, name: text })}
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Descripción</Text>
            <Input
              placeholder="Descripción detallada de la variable"
              value={formData.description}
              onChangeText={(text) => setFormData({ ...formData, description: text })}
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Tipo de Variable</Text>
            <Select
              options={variableTypeOptions}
              value={formData.type}
              onSelect={(option) => setFormData({ ...formData, type: option.value as string })}
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Unidad de Medida</Text>
            <Input
              placeholder="Ej: °C, %, pH"
              value={formData.unit}
              onChangeText={(text) => setFormData({ ...formData, unit: text })}
            />
          </View>

          <View style={styles.rangeContainer}>
            <View style={[styles.fieldContainer, styles.rangeInput]}>
              <Text style={styles.fieldLabel}>Valor Mínimo</Text>
              <Input
                placeholder="0"
                value={formData.minValue}
                onChangeText={(text) => setFormData({ ...formData, minValue: text })}
              />
            </View>
            
            <View style={[styles.fieldContainer, styles.rangeInput]}>
              <Text style={styles.fieldLabel}>Valor Máximo</Text>
              <Input
                placeholder="100"
                value={formData.maxValue}
                onChangeText={(text) => setFormData({ ...formData, maxValue: text })}
              />
            </View>
          </View>
        </View>
      </BottomSheet>

      {/* Bottom Sheet para Confirmación */}
      <BottomSheet
        isVisible={isConfirmVisible}
        onClose={() => setIsConfirmVisible(false)}
        title="Confirmar Eliminación"
        height="auto"
        footer={
          <View style={styles.footerButtons}>
            <Button
              onPress={() => setIsConfirmVisible(false)}
              variant="secondary"
              style={styles.footerButton}
            >
              Cancelar
            </Button>
            <Button
              onPress={handleDeleteConfirm}
              variant="danger"
              style={styles.footerButton}
            >
              Eliminar
            </Button>
          </View>
        }
      >
        <View style={styles.confirmContainer}>
          <Text style={styles.confirmText}>
            ¿Estás seguro de que deseas eliminar esta variable?
          </Text>
          <Text style={styles.confirmSubtext}>
            Esta acción no se puede deshacer.
          </Text>
        </View>
      </BottomSheet>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: spacing.lg,
    backgroundColor: semanticColors.backgroundPrimary,
  },
  title: {
    fontSize: typography.fontSize['2xl'],
    fontWeight: typography.fontWeight.bold as any,
    color: semanticColors.textPrimary,
    marginBottom: spacing.sm,
  },
  subtitle: {
    fontSize: typography.fontSize.md,
    color: semanticColors.textSecondary,
    marginBottom: spacing.xl,
  },
  buttonContainer: {
    gap: spacing.md,
  },
  button: {
    marginBottom: spacing.md,
  },
  formContainer: {
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

  rangeContainer: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  rangeInput: {
    flex: 1,
  },
  footerButtons: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  footerButton: {
    flex: 1,
  },
  confirmContainer: {
    paddingVertical: spacing.lg,
  },
  confirmText: {
    fontSize: typography.fontSize.md,
    color: semanticColors.textPrimary,
    marginBottom: spacing.sm,
    textAlign: 'center',
  },
  confirmSubtext: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
    textAlign: 'center',
  },
});