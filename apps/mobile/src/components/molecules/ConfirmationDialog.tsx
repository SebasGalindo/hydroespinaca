import React from 'react';
import { View, Modal, StyleSheet, TouchableOpacity } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { hapticWarning } from '../../utils/haptics';

export interface ConfirmationDialogProps {
  visible: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  testID?: string;
}

export function ConfirmationDialog({
  visible,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  destructive = false,
  onConfirm,
  onCancel,
  testID,
}: ConfirmationDialogProps): React.ReactElement {
  const handleConfirm = () => {
    if (destructive) hapticWarning();
    onConfirm();
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onCancel}
    >
      <TouchableOpacity
        style={styles.overlay}
        activeOpacity={1}
        onPress={onCancel}
      >
        <TouchableOpacity
          style={styles.dialog}
          activeOpacity={1}
          testID={testID}
        >
          {destructive && (
            <View style={styles.iconContainer}>
              <Icon name="alert-triangle" size={40} color={colors.error[500]} />
            </View>
          )}
          <Text variant="h3" color={semanticColors.textPrimary} align="center" style={styles.title}>
            {title}
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} align="center" style={styles.message}>
            {message}
          </Text>
          <View style={styles.actions}>
            <Button
              variant="outline"
              onPress={onCancel}
              style={styles.actionButton}
            >
              {cancelLabel}
            </Button>
            <Button
              variant={destructive ? 'danger' : 'primary'}
              onPress={handleConfirm}
              style={styles.actionButton}
            >
              {confirmLabel}
            </Button>
          </View>
        </TouchableOpacity>
      </TouchableOpacity>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: semanticColors.overlay,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
  dialog: {
    width: '100%',
    maxWidth: 340,
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.xl,
    padding: spacing.lg,
    alignItems: 'center',
  },
  iconContainer: {
    marginBottom: spacing.md,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    marginBottom: spacing.sm,
  },
  message: {
    marginBottom: spacing.lg,
    lineHeight: 22,
  },
  actions: {
    flexDirection: 'row',
    gap: spacing.md,
    width: '100%',
  },
  actionButton: {
    flex: 1,
  },
});
