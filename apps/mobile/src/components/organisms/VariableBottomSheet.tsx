import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, typography } from '@hydroespinaca/shared';
import { BottomSheet } from './BottomSheet';
import { VariableForm, VariableFormData } from './VariableForm';
import { Button } from '../atoms/Button';
import { Text } from '../atoms/Text';

export interface VariableBottomSheetProps {
  /** Si el bottom sheet está visible */
  isVisible: boolean;
  /** Función para cerrar el bottom sheet */
  onClose: () => void;
  /** Datos iniciales para edición */
  initialData?: Partial<VariableFormData> | undefined;
  /** Si está en modo edición */
  isEditing?: boolean;
  /** Función llamada al confirmar el formulario */
  onConfirm: (data: VariableFormData) => void;
  /** Título del bottom sheet */
  title?: string;
}

export function VariableBottomSheet({
  isVisible,
  onClose,
  initialData,
  isEditing = false,
  onConfirm,
  title,
}: VariableBottomSheetProps): React.ReactElement {
  
  const defaultTitle = isEditing ? 'Editar Variable' : 'Crear Nueva Variable';

  const handleSubmit = (data: VariableFormData) => {
    onConfirm(data);
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
          <Text style={styles.title}>{title || defaultTitle}</Text>
        </View>

        {/* Form */}
        <View style={styles.formContainer}>
          <VariableForm
            initialData={initialData || undefined}
            onSubmit={handleSubmit}
            onCancel={onClose}
            isEditing={isEditing}
          />
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
    fontWeight: typography.fontWeight.bold as any,
    color: semanticColors.textPrimary,
    textAlign: 'center',
  },

  formContainer: {
    flex: 1,
    marginBottom: spacing.xl,
  },

  actions: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingTop: spacing.lg,
  },

  cancelButton: {
    flex: 1,
  },

  confirmButton: {
    flex: 1,
  },
});