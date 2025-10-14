import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { semanticColors, spacing, typography } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { BottomSheet } from './BottomSheet';
import { ActuadorData } from './ActuadorBottomSheet';

export interface DeleteActuadorConfirmationBottomSheetProps {
  isVisible: boolean;
  onClose: () => void;
  onConfirm: () => void;
  actuador: ActuadorData | null;
}

export function DeleteActuadorConfirmationBottomSheet({
  isVisible,
  onClose,
  onConfirm,
  actuador
}: DeleteActuadorConfirmationBottomSheetProps): React.ReactElement {
  if (!actuador) return <></>;

  const getActuadorTypeLabel = (type: string) => {
    const typeLabels: Record<string, string> = {
      heater: 'Calentador',
      fan: 'Ventilador',
      led: 'LED',
      pump: 'Bomba'
    };
    return typeLabels[type] || type;
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
          <Icon 
            name="warning" 
            size={32} 
            color={semanticColors.warningText} 
            style={styles.warningIcon}
          />
          <Text variant="h2" style={styles.title}>
            Eliminar Actuador
          </Text>
        </View>

        {/* Content */}
        <View style={styles.content}>
          <Text variant="body" style={styles.message}>
            ¿Estás seguro de que deseas eliminar este actuador?
          </Text>
          
          <View style={styles.actuadorInfo}>
            <Text variant="label" style={styles.actuadorLabel}>Actuador:</Text>
            <Text variant="body" style={styles.actuadorName}>
              {actuador.nombre}
            </Text>
            <Text variant="caption" style={styles.actuadorType}>
              {getActuadorTypeLabel(actuador.tipoActuador)} - {actuador.ubicacion}
            </Text>
          </View>

          <Text variant="caption" style={styles.warningText}>
            Esta acción no se puede deshacer. Se perderán todas las configuraciones del actuador.
          </Text>
        </View>

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
            variant="danger"
            onPress={onConfirm}
            style={[styles.button, styles.deleteButton]}
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
    alignItems: 'center',
  },

  warningIcon: {
    marginBottom: spacing.sm,
  },

  title: {
    fontSize: typography.fontSize.xl,
    fontWeight: 'bold' as const,
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
    marginBottom: spacing.lg,
  },

  actuadorInfo: {
    backgroundColor: semanticColors.backgroundSecondary,
    padding: spacing.md,
    borderRadius: 8,
    marginBottom: spacing.lg,
    alignItems: 'center',
    minWidth: '80%',
  },

  actuadorLabel: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
    marginBottom: spacing.xs,
  },

  actuadorName: {
    fontSize: typography.fontSize.lg,
    fontWeight: '500' as const,
    color: semanticColors.textPrimary,
    marginBottom: spacing.xs,
  },

  actuadorType: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.textSecondary,
  },

  warningText: {
    fontSize: typography.fontSize.sm,
    color: semanticColors.warningText,
    textAlign: 'center',
    fontStyle: 'italic',
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

  deleteButton: {
    flex: 1,
    backgroundColor: semanticColors.errorText,
  } as ViewStyle,
});