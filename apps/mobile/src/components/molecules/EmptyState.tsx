import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { spacing, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';

export interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  style?: ViewStyle;
  testID?: string;
}

export const EmptyState = React.memo(function EmptyState({
  icon = 'file-text',
  title,
  description,
  actionLabel,
  onAction,
  style,
  testID,
}: EmptyStateProps): React.ReactElement {
  return (
    <View style={[styles.container, style]} testID={testID}>
      <Icon name={icon as any} size={56} color={semanticColors.textTertiary} />
      <Text variant="h3" color={semanticColors.textSecondary} align="center" style={styles.title}>
        {title}
      </Text>
      {description && (
        <Text variant="body" color={semanticColors.textTertiary} align="center" style={styles.description}>
          {description}
        </Text>
      )}
      {actionLabel && onAction && (
        <Button variant="primary" onPress={onAction} style={styles.action}>
          {actionLabel}
        </Button>
      )}
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.sm,
  },
  title: {
    marginTop: spacing.md,
    fontWeight: typography.fontWeight.semibold,
  },
  description: {
    marginTop: spacing.xs,
    maxWidth: 280,
  },
  action: {
    marginTop: spacing.lg,
  },
});
