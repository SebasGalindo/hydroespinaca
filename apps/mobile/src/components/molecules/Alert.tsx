import React from 'react';
import { View, StyleSheet, ViewStyle, TouchableOpacity } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';

export interface AlertProps {
  type: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  message: string;
  onDismiss?: () => void;
  style?: ViewStyle;
  testID?: string;
}

const alertConfig = {
  info: {
    bg: colors.info[50],
    border: colors.info[200],
    iconColor: colors.info[600],
    textColor: colors.info[800],
    icon: 'info',
  },
  success: {
    bg: colors.hidro[50],
    border: colors.hidro[200],
    iconColor: colors.hidro[600],
    textColor: colors.hidro[800],
    icon: 'check-circle',
  },
  warning: {
    bg: colors.warning[50],
    border: colors.warning[200],
    iconColor: colors.warning[600],
    textColor: colors.warning[800],
    icon: 'warning',
  },
  error: {
    bg: colors.error[50],
    border: colors.error[200],
    iconColor: colors.error[600],
    textColor: colors.error[800],
    icon: 'alert-triangle',
  },
} as const;

export const Alert = React.memo(function Alert({
  type,
  title,
  message,
  onDismiss,
  style,
  testID,
}: AlertProps): React.ReactElement {
  const config = alertConfig[type];

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: config.bg,
          borderColor: config.border,
        },
        style,
      ]}
      testID={testID}
      accessibilityRole="alert"
    >
      <Icon name={config.icon as any} size={20} color={config.iconColor} />
      <View style={styles.textContainer}>
        {title && (
          <Text variant="label" color={config.textColor} style={styles.title}>
            {title}
          </Text>
        )}
        <Text variant="caption" color={config.textColor}>
          {message}
        </Text>
      </View>
      {onDismiss && (
        <TouchableOpacity
          onPress={onDismiss}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          accessibilityRole="button"
          accessibilityLabel="Cerrar alerta"
        >
          <Icon name="close" size={18} color={config.iconColor} />
        </TouchableOpacity>
      )}
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    borderWidth: 1,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    gap: spacing.sm,
  },
  textContainer: {
    flex: 1,
  },
  title: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: 2,
  },
});
