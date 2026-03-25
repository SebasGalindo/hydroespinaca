import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { View, StyleSheet } from 'react-native';
import { BottomSheetModal, BottomSheetBackdrop } from '@gorhom/bottom-sheet';
import type { BottomSheetBackdropProps } from '@gorhom/bottom-sheet';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { hapticWarning } from '../../utils/haptics';

export interface ConfirmationSheetProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  testID?: string;
}

export function ConfirmationSheet({
  isOpen,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  destructive = false,
  onConfirm,
  onCancel,
  testID,
}: ConfirmationSheetProps): React.ReactElement | null {
  const bottomSheetRef = useRef<BottomSheetModal>(null);
  const dismissReasonRef = useRef<'confirm' | 'cancel'>('cancel');

  const snapPoints = useMemo(() => ['35%'], []);

  useEffect(() => {
    if (isOpen) {
      dismissReasonRef.current = 'cancel';
      bottomSheetRef.current?.present();
    } else {
      bottomSheetRef.current?.dismiss();
    }
  }, [isOpen]);

  const handleConfirm = useCallback(() => {
    if (destructive) hapticWarning();
    dismissReasonRef.current = 'confirm';
    bottomSheetRef.current?.dismiss();
  }, [destructive]);

  const handleCancel = useCallback(() => {
    dismissReasonRef.current = 'cancel';
    bottomSheetRef.current?.dismiss();
  }, []);

  const handleDismiss = useCallback(() => {
    const reason = dismissReasonRef.current;
    dismissReasonRef.current = 'cancel';
    if (reason === 'confirm') {
      onConfirm();
    } else {
      onCancel();
    }
  }, [onConfirm, onCancel]);

  const renderBackdrop = useCallback(
    (props: BottomSheetBackdropProps) => (
      <BottomSheetBackdrop
        {...props}
        disappearsOnIndex={-1}
        appearsOnIndex={0}
        opacity={0.5}
        pressBehavior="close"
      />
    ),
    []
  );

  return (
    <BottomSheetModal
      ref={bottomSheetRef}
      snapPoints={snapPoints}
      onDismiss={handleDismiss}
      enablePanDownToClose
      backdropComponent={renderBackdrop}
      handleIndicatorStyle={styles.handle}
      backgroundStyle={styles.background}
    >
      <View style={styles.content} testID={testID}>
        {destructive && (
          <View style={styles.iconContainer}>
            <Icon name="alert-triangle" size={44} color={colors.error[500]} />
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
            onPress={handleCancel}
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
      </View>
    </BottomSheetModal>
  );
}

const styles = StyleSheet.create({
  background: {
    backgroundColor: semanticColors.surface,
    borderTopLeftRadius: borderRadius.xl,
    borderTopRightRadius: borderRadius.xl,
  },
  handle: {
    backgroundColor: colors.gray[300],
    width: 40,
  },
  content: {
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
    maxWidth: 300,
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
