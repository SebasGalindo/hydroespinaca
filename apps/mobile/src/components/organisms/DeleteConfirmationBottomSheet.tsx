import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, typography } from '@hydroespinaca/shared';
import { BottomSheet } from './BottomSheet';
import { Button } from '../atoms/Button';
import { Text } from '../atoms/Text';

export interface DeleteConfirmationBottomSheetProps {
  /** Si el bottom sheet está visible */
  isVisible: boolean;
  /** Función para cerrar el bottom sheet */
  onClose: () => void;
  /** Función llamada al confirmar la eliminación */
  onConfirm: () => void;
  /** Nombre de la variable a eliminar */
  variableName?: string | undefined;
}

export function DeleteConfirmationBottomSheet({
  isVisible,
  onClose,
  onConfirm,
  variableName,
}: DeleteConfirmationBottomSheetProps): React.ReactElement {

  const handleConfirm = () => {
    onConfirm();
    onClose();
  };

  return (
    <BottomSheet 
      isVisible={isVisible} 
      onClose={onClose}
      height={0.4} // 40% de la altura de la pantalla
    >
      <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>Eliminar Variable</Text>
        </View>

        {/* Content */}
        <View style={styles.content}>
          <Text style={styles.message}>
            ¿Estás seguro de que deseas eliminar la variable{' '}
            <Text style={styles.variableName}>"{variableName}"</Text>?
          </Text>
          
          <Text style={styles.warning}>
            Esta acción no se puede deshacer.
          </Text>
        </View>

        {/* Actions */}
        <View style={styles.actions}>
          <Button
            variant="secondary"
            onPress={onClose}
            style={styles.cancelButton}
          >
            Cancelar
          </Button>
          
          <Button
            variant="danger"
            onPress={handleConfirm}
            style={styles.deleteButton}
          >
            Eliminar
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
    justifyContent: 'space-between',
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

  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: spacing.lg,
  },

  message: {
    fontSize: typography.fontSize.md,
    color: semanticColors.textPrimary,
    textAlign: 'center',
    marginBottom: spacing.md,
    lineHeight: 24,
  },

  variableName: {
    fontWeight: typography.fontWeight.bold as any,
    color: semanticColors.primary,
  },

  warning: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.errorText,
    textAlign: 'center',
    fontStyle: 'italic',
  },

  actions: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingTop: spacing.lg,
  },

  cancelButton: {
    flex: 1,
  },

  deleteButton: {
    flex: 1,
  },
});