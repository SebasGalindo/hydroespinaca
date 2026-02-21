import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import type { BadgeProps } from '../atoms/Badge';
import {
  colors, spacing, semanticColors,
  CHANNEL_LABELS, CHANNEL_ICONS,
  type NotificationLogEntry, type NotificationChannel,
} from '@hydroespinaca/shared';

export interface NotificationHistoryItemProps {
  entry: NotificationLogEntry;
  style?: ViewStyle;
  testID?: string;
}

const STATUS_MAP: Record<string, { label: string; variant: BadgeProps['variant'] }> = {
  sent: { label: 'Enviado', variant: 'success' },
  delivered: { label: 'Entregado', variant: 'success' },
  failed: { label: 'Fallido', variant: 'error' },
  pending: { label: 'Pendiente', variant: 'warning' },
  queued: { label: 'En cola', variant: 'info' },
};

function formatDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  const day = d.getDate().toString().padStart(2, '0');
  const month = (d.getMonth() + 1).toString().padStart(2, '0');
  const hours = d.getHours().toString().padStart(2, '0');
  const mins = d.getMinutes().toString().padStart(2, '0');
  return `${day}/${month} ${hours}:${mins}`;
}

export function NotificationHistoryItem({
  entry,
  style,
  testID,
}: NotificationHistoryItemProps): React.ReactElement {
  const channelKey = entry.channel as NotificationChannel;
  const channelIcon = CHANNEL_ICONS[channelKey] ?? '📨';
  const channelLabel = CHANNEL_LABELS[channelKey] ?? entry.channel;
  const statusInfo = STATUS_MAP[entry.status] ?? { label: entry.status, variant: 'default' as const };

  return (
    <View style={[styles.container, style]} testID={testID}>
      <Text variant="body" style={styles.channelIcon}>{channelIcon}</Text>

      <View style={styles.content}>
        <View style={styles.topRow}>
          <Text
            variant="body"
            weight="medium"
            color={semanticColors.textPrimary}
            numberOfLines={1}
            style={styles.title}
          >
            {entry.title ?? entry.templateKey ?? channelLabel}
          </Text>
          <Badge
            variant={statusInfo.variant}
            size="sm"
          >
            {statusInfo.label}
          </Badge>
        </View>

        <View style={styles.bottomRow}>
          <Text variant="caption" color={semanticColors.textSecondary}>
            {channelLabel}
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary}>
            {formatDate(entry.sentAt ?? entry.createdAt)}
          </Text>
        </View>

        {entry.error && (
          <Text variant="caption" color={semanticColors.error} numberOfLines={2}>
            {entry.error}
          </Text>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.gray[100],
    backgroundColor: colors.white,
  },
  channelIcon: {
    marginRight: spacing.sm,
    marginTop: 2,
    fontSize: 20,
  },
  content: {
    flex: 1,
  },
  topRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 2,
  },
  title: {
    flex: 1,
    marginRight: spacing.xs,
  },
  bottomRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
});
