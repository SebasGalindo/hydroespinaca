import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';

export interface DisconnectionBannerProps {
  message?: string;
  onRetry?: () => void;
  style?: ViewStyle;
  testID?: string;
}

export function DisconnectionBanner({
  message = 'Sin conexión al servidor. Reintentando automáticamente...',
  onRetry,
  style,
  testID,
}: DisconnectionBannerProps): React.ReactElement {
  return (
    <View style={[styles.container, style]} testID={testID} accessibilityRole="alert">
      <View style={styles.content}>
        <Icon name="warning" size={20} color={semanticColors.warningIcon} />
        <View style={styles.textContainer}>
          <Text variant="label" color={semanticColors.warningText} style={styles.title}>
            Actualizaciones pausadas
          </Text>
          <Text variant="caption" color={semanticColors.dangerIcon}>
            {message}
          </Text>
          {onRetry && (
            <Button
              onPress={onRetry}
              variant="primary"
              size="sm"
              style={styles.button}
              leftIcon={<Icon name="refresh" size={16} color={colors.white} />}
            >
              Reintentar
            </Button>
          )}
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.warningBgLight,
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: semanticColors.warningBorderLight,
    padding: spacing.md,
  },
  content: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
  },
  textContainer: {
    flex: 1,
  },
  title: {
    fontWeight: typography.fontWeight.semibold,
    marginBottom: spacing.xs,
  },
  button: {
    alignSelf: 'flex-start',
    marginTop: spacing.sm,
  },
});
