import React, { useCallback, useMemo, useRef } from 'react';
import { View, StyleSheet, TouchableOpacity, Keyboard } from 'react-native';
import BottomSheet, { BottomSheetScrollView, BottomSheetBackdrop } from '@gorhom/bottom-sheet';
import type { BottomSheetBackdropProps } from '@gorhom/bottom-sheet';
import { colors, spacing, semanticColors, borderRadius, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';

export interface BottomSheetFormProps {
  title: string;
  isOpen: boolean;
  onClose: () => void;
  children: React.ReactNode;
  snapPoints?: (string | number)[];
  testID?: string;
}

export function BottomSheetForm({
  title,
  isOpen,
  onClose,
  children,
  snapPoints: customSnapPoints,
  testID,
}: BottomSheetFormProps): React.ReactElement | null {
  const bottomSheetRef = useRef<BottomSheet>(null);

  const snapPoints = useMemo(
    () => customSnapPoints || ['60%', '90%'],
    [customSnapPoints]
  );

  const handleClose = useCallback(() => {
    Keyboard.dismiss();
    bottomSheetRef.current?.close();
    onClose();
  }, [onClose]);

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

  if (!isOpen) return null;

  return (
    <BottomSheet
      ref={bottomSheetRef}
      index={0}
      snapPoints={snapPoints}
      onClose={handleClose}
      enablePanDownToClose
      backdropComponent={renderBackdrop}
      handleIndicatorStyle={styles.handle}
      backgroundStyle={styles.background}
      keyboardBehavior="interactive"
      keyboardBlurBehavior="restore"
      android_keyboardInputMode="adjustResize"
    >
      <View style={styles.header} testID={testID}>
        <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
          {title}
        </Text>
        <TouchableOpacity
          onPress={handleClose}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          accessibilityLabel="Cerrar formulario"
          accessibilityRole="button"
        >
          <Icon name="close" size={24} color={semanticColors.textSecondary} />
        </TouchableOpacity>
      </View>
      <BottomSheetScrollView
        contentContainerStyle={styles.content}
        keyboardShouldPersistTaps="handled"
      >
        {children}
      </BottomSheetScrollView>
    </BottomSheet>
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
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[200],
  },
  title: {
    fontWeight: typography.fontWeight.bold,
    flex: 1,
  },
  content: {
    padding: spacing.lg,
    paddingBottom: spacing.xl * 2,
  },
});
